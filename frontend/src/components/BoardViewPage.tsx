import { useState } from "react";
import { useParams, Link, useNavigate } from "react-router-dom";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import {
  DndContext,
  DragEndEvent,
  DragOverEvent,
  PointerSensor,
  closestCorners,
  useDroppable,
  useSensor,
  useSensors,
} from "@dnd-kit/core";
import { SortableContext, useSortable, verticalListSortingStrategy } from "@dnd-kit/sortable";
import { CSS } from "@dnd-kit/utilities";
import { useSignalR } from "../hooks/useSignalR";
import {
  getBoardDetail,
  createList,
  createCard,
  moveCard,
  inviteMember,
  createLabel,
} from "../api/boards";
import { useAuthStore } from "../store/authStore";
import CardDetailModal from "./CardDetailModal";
import type {
  CardMovedEvent,
  BoardDetailDto,
  BoardListDto,
  CardDto,
  LabelDto,
} from "../types";

function listDropId(listId: string) {
  return `list:${listId}`;
}

function parseListId(id: string): string | null {
  return id.startsWith("list:") ? id.slice(5) : null;
}

function SortableCard({
  card,
  labels,
  onOpen,
}: {
  card: CardDto;
  labels: LabelDto[];
  onOpen: () => void;
}) {
  const { attributes, listeners, setNodeRef, transform, transition, isDragging } = useSortable({
    id: card.id,
    data: { type: "card", card },
  });

  const style = {
    transform: CSS.Transform.toString(transform),
    transition,
    opacity: isDragging ? 0.5 : 1,
  };

  const cardLabels = labels.filter((l) => card.labelIds?.includes(l.id));

  return (
    <div
      ref={setNodeRef}
      style={style}
      className="kanban-card group cursor-grab active:cursor-grabbing"
      {...attributes}
      {...listeners}
      onClick={(e) => {
        // Avoid opening while finishing a drag
        if (isDragging) return;
        e.stopPropagation();
        onOpen();
      }}
    >
      {cardLabels.length > 0 && (
        <div className="flex flex-wrap gap-1 mb-1.5">
          {cardLabels.map((l) => (
            <span
              key={l.id}
              className="h-1.5 w-8 rounded-full"
              style={{ backgroundColor: l.color }}
              title={l.name}
            />
          ))}
        </div>
      )}
      <div className="font-medium text-slate-200">{card.title}</div>
      {card.description && (
        <p className="text-xs text-slate-400 mt-1 line-clamp-2">{card.description}</p>
      )}
      <div className="flex items-center gap-2 mt-2 text-[10px] text-slate-500">
        {card.assigneeName && <span>{card.assigneeName}</span>}
        {card.dueDateUtc && (
          <span>Due {new Date(card.dueDateUtc).toLocaleDateString()}</span>
        )}
      </div>
    </div>
  );
}

function DroppableListBody({
  listId,
  children,
}: {
  listId: string;
  children: React.ReactNode;
}) {
  const { setNodeRef, isOver } = useDroppable({
    id: listDropId(listId),
    data: { type: "list", listId },
  });

  return (
    <div
      ref={setNodeRef}
      className={`kanban-list-body min-h-[40px] rounded-lg transition-colors ${
        isOver ? "bg-indigo-500/10" : ""
      }`}
    >
      {children}
    </div>
  );
}

function roleLabel(role: string | number) {
  if (role === 2 || role === "Admin") return "Admin";
  if (role === 1 || role === "Editor") return "Editor";
  return "Viewer";
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
  const [selectedCardId, setSelectedCardId] = useState<string | null>(null);
  const [showMembers, setShowMembers] = useState(false);
  const [inviteEmail, setInviteEmail] = useState("");
  const [inviteRole, setInviteRole] = useState(1);
  const [inviteError, setInviteError] = useState<string | null>(null);

  const sensors = useSensors(
    useSensor(PointerSensor, { activationConstraint: { distance: 6 } })
  );

  const { data: board, isLoading, isError } = useQuery({
    queryKey: ["board", boardId],
    queryFn: () => getBoardDetail(boardId!),
    enabled: !!boardId,
  });

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
    onBoardChanged: () => {
      queryClient.invalidateQueries({ queryKey: ["board", boardId] });
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

  const inviteMutation = useMutation({
    mutationFn: () => inviteMember(boardId!, inviteEmail.trim(), inviteRole),
    onSuccess: () => {
      setInviteEmail("");
      setInviteError(null);
      queryClient.invalidateQueries({ queryKey: ["board", boardId] });
    },
    onError: () => setInviteError("Invite failed. User must already be registered."),
  });

  function findListOfCard(cardId: string, lists: BoardListDto[]) {
    return lists.find((l) => l.cards.some((c) => c.id === cardId));
  }

  function resolveTargetListId(overId: string, lists: BoardListDto[]): string | null {
    const asList = parseListId(overId);
    if (asList) return asList;
    return findListOfCard(overId, lists)?.id ?? null;
  }

  function resolveNewOrder(
    lists: BoardListDto[],
    cardId: string,
    targetListId: string,
    overId: string
  ): number {
    const target = lists.find((l) => l.id === targetListId);
    if (!target) return 0;

    const withoutActive = target.cards.filter((c) => c.id !== cardId);
    if (parseListId(overId) === targetListId) {
      return withoutActive.length;
    }

    const overIndex = withoutActive.findIndex((c) => c.id === overId);
    return overIndex >= 0 ? overIndex : withoutActive.length;
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
        inserted.splice(Math.min(newOrder, inserted.length), 0, {
          ...card,
          order: newOrder,
        });
        return { ...list, cards: inserted };
      }
      return list;
    });
  }

  async function commitMove(cardId: string, targetListId: string, newOrder: number) {
    if (!boardId || !board) return;
    const source = findListOfCard(cardId, board.lists);
    if (!source) return;
    if (source.id === targetListId) {
      const currentIndex = source.cards.findIndex((c) => c.id === cardId);
      if (currentIndex === newOrder) return;
    }

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

  async function handleDragEnd(event: DragEndEvent) {
    const { active, over } = event;
    if (!over || !board) return;

    const cardId = String(active.id);
    const overId = String(over.id);
    const targetListId = resolveTargetListId(overId, board.lists);
    if (!targetListId) return;

    const newOrder = resolveNewOrder(board.lists, cardId, targetListId, overId);
    await commitMove(cardId, targetListId, newOrder);
  }

  function handleDragOver(event: DragOverEvent) {
    const { active, over } = event;
    if (!over || !board || !boardId) return;

    const cardId = String(active.id);
    const overId = String(over.id);
    const sourceList = findListOfCard(cardId, board.lists);
    const targetListId = resolveTargetListId(overId, board.lists);
    if (!sourceList || !targetListId || sourceList.id === targetListId) return;

    const newOrder = resolveNewOrder(board.lists, cardId, targetListId, overId);
    queryClient.setQueryData<BoardDetailDto>(["board", boardId], (old) => {
      if (!old) return old;
      return {
        ...old,
        lists: applyCardMove(old.lists, cardId, targetListId, newOrder),
      };
    });
  }

  const selectedCard =
    selectedCardId && board
      ? board.lists.flatMap((l) => l.cards).find((c) => c.id === selectedCardId) ?? null
      : null;

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
            The board you are looking for might have been deleted or you don't have permission to
            access it.
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
      <header className="navbar flex items-center justify-between">
        <div className="flex items-center gap-4">
          <Link
            to="/boards"
            className="text-slate-400 hover:text-white transition-colors text-sm font-medium"
          >
            ← Boards
          </Link>
          <span className="text-slate-600">/</span>
          <h1 className="text-base font-bold text-slate-100">{board.title}</h1>
        </div>

        <div className="flex items-center gap-3">
          <button
            onClick={() => setShowMembers((v) => !v)}
            className="tf-btn tf-btn-ghost text-xs py-1.5 px-3"
          >
            Members ({board.members.length})
          </button>
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

      <div className="p-6 pb-2 flex items-start justify-between gap-4">
        <div>
          <h2 className="text-2xl font-bold tracking-tight">{board.title}</h2>
          {board.description && (
            <p className="text-sm text-slate-400 mt-0.5">{board.description}</p>
          )}
        </div>

        {showMembers && (
          <div className="glass rounded-xl p-4 w-80 flex-shrink-0 space-y-3">
            <h3 className="text-sm font-semibold">Board members</h3>
            <ul className="space-y-1.5 text-sm max-h-40 overflow-y-auto">
              {board.members.map((m) => (
                <li key={m.userId} className="flex justify-between gap-2">
                  <span>{m.displayName}</span>
                  <span className="text-xs text-slate-500">{roleLabel(m.role)}</span>
                </li>
              ))}
            </ul>
            <form
              className="space-y-2 border-t border-white/10 pt-3"
              onSubmit={(e) => {
                e.preventDefault();
                if (inviteEmail.trim()) inviteMutation.mutate();
              }}
            >
              <p className="text-xs text-slate-400">Invite by registered email (Admin only)</p>
              <input
                className="tf-input text-xs"
                placeholder="colleague@example.com"
                value={inviteEmail}
                onChange={(e) => setInviteEmail(e.target.value)}
              />
              <select
                className="tf-input text-xs"
                value={inviteRole}
                onChange={(e) => setInviteRole(Number(e.target.value))}
              >
                <option value={0}>Viewer</option>
                <option value={1}>Editor</option>
                <option value={2}>Admin</option>
              </select>
              {inviteError && <p className="text-xs text-rose-400">{inviteError}</p>}
              <button
                type="submit"
                className="tf-btn tf-btn-primary text-xs py-1.5 px-3 w-full"
                disabled={inviteMutation.isPending}
              >
                Invite
              </button>
            </form>
          </div>
        )}
      </div>

      <div className="flex-1 p-6 flex gap-5 items-start overflow-x-auto">
        <DndContext
          sensors={sensors}
          collisionDetection={closestCorners}
          onDragOver={handleDragOver}
          onDragEnd={handleDragEnd}
        >
          {board.lists.map((list) => (
            <div key={list.id} className="kanban-list">
              <div className="kanban-list-header">
                <h3 className="font-semibold text-sm text-slate-200">{list.title}</h3>
                <span className="text-xs text-slate-500 font-mono px-2 py-0.5 rounded bg-slate-800">
                  {list.cards.length}
                </span>
              </div>

              <DroppableListBody listId={list.id}>
                <SortableContext
                  items={list.cards.map((c) => c.id)}
                  strategy={verticalListSortingStrategy}
                >
                  {list.cards.map((card) => (
                    <SortableCard
                      key={card.id}
                      card={card}
                      labels={board.labels}
                      onOpen={() => setSelectedCardId(card.id)}
                    />
                  ))}
                </SortableContext>

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
              </DroppableListBody>
            </div>
          ))}
        </DndContext>

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

      {selectedCard && (
        <CardDetailModal
          boardId={boardId!}
          card={selectedCard}
          labels={board.labels}
          members={board.members}
          onClose={() => setSelectedCardId(null)}
          onCreateLabel={async (name, color) => {
            await createLabel(boardId!, name, color);
            await queryClient.invalidateQueries({ queryKey: ["board", boardId] });
          }}
        />
      )}
    </div>
  );
}
