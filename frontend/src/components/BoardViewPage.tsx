import { useState } from "react";
import { useParams, Link, useNavigate } from "react-router-dom";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import {
  DndContext,
  DragEndEvent,
  PointerSensor,
  useSensor,
  useSensors,
} from "@dnd-kit/core";
import { SortableContext, useSortable, verticalListSortingStrategy } from "@dnd-kit/sortable";
import { CSS } from "@dnd-kit/utilities";
import { useSignalR } from "../hooks/useSignalR";
import { getBoardDetail, createList, createCard, moveCard } from "../api/boards";
import { useAuthStore } from "../store/authStore";
import type { CardMovedEvent, BoardDetailDto, BoardListDto, CardDto } from "../types";

function SortableCard({ card }: { card: CardDto }) {
  const { attributes, listeners, setNodeRef, transform, transition } = useSortable({
    id: card.id,
  });

  const style = {
    transform: CSS.Transform.toString(transform),
    transition,
  };

  return (
    <div
      ref={setNodeRef}
      style={style}
      {...attributes}
      {...listeners}
      className="kanban-card group"
    >
      <div className="font-medium text-slate-200">{card.title}</div>
      {card.description && (
        <p className="text-xs text-slate-400 mt-1 line-clamp-2">{card.description}</p>
      )}
    </div>
  );
}

export default function BoardViewPage() {
  const { boardId } = useParams<{ boardId: string }>();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const clearAuth = useAuthStore((s) => s.clear);

  const [newListTitle, setNewListTitle] = useState("");
  const [isAddingList, setIsAddingList] = useState(false);

  const [activeAddingCardListId, setActiveAddingCardListId] = useState<string | null>(null);
  const [newCardTitle, setNewCardTitle] = useState("");

  const sensors = useSensors(useSensor(PointerSensor));

  // Fetch real board data
  const { data: board, isLoading, isError } = useQuery({
    queryKey: ["board", boardId],
    queryFn: () => getBoardDetail(boardId!),
    enabled: !!boardId,
  });

  // SignalR real-time synchronization
  useSignalR({
    boardId,
    onCardMoved: (event: CardMovedEvent) => {
      queryClient.setQueryData<BoardDetailDto>(["board", boardId], (old) => {
        if (!old) return old;
        return {
          ...old,
          lists: applyCardMove(old.lists, event.cardId, event.targetListId, event.newOrder),
        };
      });
    },
  });

  const createListMutation = useMutation({
    mutationFn: (title: string) => createList(boardId!, title),
    onSuccess: () => {
      setNewListTitle("");
      setIsAddingList(false);
      queryClient.invalidateQueries({ queryKey: ["board", boardId] });
    },
  });

  const createCardMutation = useMutation({
    mutationFn: ({ listId, title }: { listId: string; title: string }) =>
      createCard(boardId!, listId, title),
    onSuccess: () => {
      setNewCardTitle("");
      setActiveAddingCardListId(null);
      queryClient.invalidateQueries({ queryKey: ["board", boardId] });
    },
  });

  function findListOfCard(cardId: string, lists: BoardListDto[]) {
    return lists.find((l) => l.cards.some((c) => c.id === cardId));
  }

  function applyCardMove(
    lists: BoardListDto[],
    cardId: string,
    targetListId: string,
    newOrder: number
  ): BoardListDto[] {
    const sourceList = findListOfCard(cardId, lists);
    const card = sourceList?.cards.find((c) => c.id === cardId);
    if (!sourceList || !card) return lists;

    return lists.map((list) => {
      if (list.id === sourceList.id && list.id !== targetListId) {
        return { ...list, cards: list.cards.filter((c) => c.id !== cardId) };
      }
      if (list.id === targetListId) {
        const withoutCard = list.cards.filter((c) => c.id !== cardId);
        const inserted = [...withoutCard];
        inserted.splice(newOrder, 0, { ...card, order: newOrder });
        return { ...list, cards: inserted };
      }
      return list;
    });
  }

  async function handleDragEnd(event: DragEndEvent) {
    const { active, over } = event;
    if (!over || !boardId || !board) return;

    const cardId = String(active.id);
    const targetListId = String(over.id);

    const sourceList = findListOfCard(cardId, board.lists);
    if (!sourceList) return;

    // Optimistically update React Query cache
    const newOrder = 0;
    queryClient.setQueryData<BoardDetailDto>(["board", boardId], (old) => {
      if (!old) return old;
      return {
        ...old,
        lists: applyCardMove(old.lists, cardId, targetListId, newOrder),
      };
    });

    try {
      await moveCard(boardId, cardId, targetListId, newOrder);
    } catch (err) {
      console.error("Failed to move card:", err);
      queryClient.invalidateQueries({ queryKey: ["board", boardId] });
    }
  }

  if (isLoading) {
    return (
      <div className="min-h-screen bg-[#0f1117] flex items-center justify-center text-slate-400">
        <div className="flex items-center gap-3">
          <div className="spinner" />
          <span>Loading board details...</span>
        </div>
      </div>
    );
  }

  if (isError || !board) {
    return (
      <div className="min-h-screen bg-[#0f1117] p-8 text-slate-200">
        <div className="max-w-md mx-auto text-center glass p-8 rounded-2xl">
          <h2 className="text-xl font-bold text-rose-400 mb-2">Board Not Found</h2>
          <p className="text-sm text-slate-400 mb-6">
            The board you are looking for might have been deleted or you don't have permission to access it.
          </p>
          <Link to="/boards" className="tf-btn tf-btn-primary">
            Back to Boards
          </Link>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-[#0f1117] text-slate-100 flex flex-col overflow-x-auto">
      {/* Top Navbar */}
      <header className="navbar flex items-center justify-between">
        <div className="flex items-center gap-4">
          <Link to="/boards" className="text-slate-400 hover:text-white transition-colors text-sm font-medium">
            ← Boards
          </Link>
          <span className="text-slate-600">/</span>
          <h1 className="text-base font-bold text-slate-100">{board.title}</h1>
        </div>

        <div className="flex items-center gap-4">
          <button
            onClick={() => {
              clearAuth();
              navigate("/login");
            }}
            className="tf-btn tf-btn-ghost text-xs py-1.5 px-3"
          >
            Log Out
          </button>
        </div>
      </header>

      {/* Board Workspace Header */}
      <div className="p-6 pb-2 flex items-center justify-between">
        <div>
          <h2 className="text-2xl font-bold tracking-tight">{board.title}</h2>
          {board.description && (
            <p className="text-sm text-slate-400 mt-0.5">{board.description}</p>
          )}
        </div>
      </div>

      {/* Kanban Board Horizontal Columns */}
      <div className="flex-1 p-6 flex gap-5 items-start overflow-x-auto">
        <DndContext sensors={sensors} onDragEnd={handleDragEnd}>
          {board.lists.map((list) => (
            <div key={list.id} id={list.id} className="kanban-list">
              <div className="kanban-list-header">
                <h3 className="font-semibold text-sm text-slate-200">{list.title}</h3>
                <span className="text-xs text-slate-500 font-mono px-2 py-0.5 rounded bg-slate-800">
                  {list.cards.length}
                </span>
              </div>

              <div className="kanban-list-body">
                <SortableContext
                  items={list.cards.map((c) => c.id)}
                  strategy={verticalListSortingStrategy}
                >
                  {list.cards.map((card) => (
                    <SortableCard key={card.id} card={card} />
                  ))}
                </SortableContext>

                {/* Add Card Form */}
                {activeAddingCardListId === list.id ? (
                  <form
                    className="mt-2 p-2 glass rounded-xl space-y-2 fade-in"
                    onSubmit={(e) => {
                      e.preventDefault();
                      if (newCardTitle.trim()) {
                        createCardMutation.mutate({ listId: list.id, title: newCardTitle });
                      }
                    }}
                  >
                    <input
                      type="text"
                      autoFocus
                      className="tf-input text-xs"
                      placeholder="Card title..."
                      value={newCardTitle}
                      onChange={(e) => setNewCardTitle(e.target.value)}
                    />
                    <div className="flex gap-2 justify-end">
                      <button
                        type="button"
                        className="text-xs text-slate-400 hover:text-slate-200 px-2 py-1"
                        onClick={() => setActiveAddingCardListId(null)}
                      >
                        Cancel
                      </button>
                      <button
                        type="submit"
                        className="tf-btn tf-btn-primary text-xs py-1 px-3"
                        disabled={createCardMutation.isPending}
                      >
                        Add
                      </button>
                    </div>
                  </form>
                ) : (
                  <button
                    onClick={() => {
                      setActiveAddingCardListId(list.id);
                      setNewCardTitle("");
                    }}
                    className="w-full mt-2 py-2 text-xs font-medium text-slate-400 hover:text-slate-200 hover:bg-white/5 rounded-lg border border-dashed border-slate-700/60 transition-colors flex items-center justify-center gap-1"
                  >
                    + Add Card
                  </button>
                )}
              </div>
            </div>
          ))}
        </DndContext>

        {/* Add List Button/Form */}
        {isAddingList ? (
          <div className="w-72 glass rounded-2xl p-4 border border-indigo-500/30 flex-shrink-0 fade-in">
            <h4 className="text-sm font-semibold text-slate-200 mb-3">Add New List</h4>
            <form
              className="space-y-3"
              onSubmit={(e) => {
                e.preventDefault();
                if (newListTitle.trim()) createListMutation.mutate(newListTitle);
              }}
            >
              <input
                type="text"
                autoFocus
                className="tf-input"
                placeholder="List title (e.g. In Progress)"
                value={newListTitle}
                onChange={(e) => setNewListTitle(e.target.value)}
              />
              <div className="flex justify-end gap-2">
                <button
                  type="button"
                  onClick={() => setIsAddingList(false)}
                  className="tf-btn tf-btn-ghost text-xs py-1.5 px-3"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  className="tf-btn tf-btn-primary text-xs py-1.5 px-3"
                  disabled={createListMutation.isPending}
                >
                  Create List
                </button>
              </div>
            </form>
          </div>
        ) : (
          <button
            onClick={() => setIsAddingList(true)}
            className="w-72 h-14 rounded-2xl glass border border-dashed border-slate-700 hover:border-indigo-500/50 hover:bg-indigo-500/5 transition-all text-sm font-medium text-slate-300 flex items-center justify-center gap-2 flex-shrink-0"
          >
            + Add List
          </button>
        )}
      </div>
    </div>
  );
}
