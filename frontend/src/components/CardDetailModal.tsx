import { FormEvent, useEffect, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  addCardComment,
  deleteCard,
  getCardComments,
  updateCard,
} from "../api/boards";
import type { CardDto, LabelDto, MemberSummaryDto } from "../types";

const LABEL_COLORS = ["#6366f1", "#10b981", "#f59e0b", "#ef4444", "#38bdf8", "#a78bfa"];

interface Props {
  boardId: string;
  card: CardDto;
  labels: LabelDto[];
  members: MemberSummaryDto[];
  onClose: () => void;
  onCreateLabel: (name: string, color: string) => Promise<void>;
}

export default function CardDetailModal({
  boardId,
  card,
  labels,
  members,
  onClose,
  onCreateLabel,
}: Props) {
  const queryClient = useQueryClient();
  const [title, setTitle] = useState(card.title);
  const [description, setDescription] = useState(card.description ?? "");
  const [assigneeId, setAssigneeId] = useState(card.assigneeId ?? "");
  const [dueDate, setDueDate] = useState(
    card.dueDateUtc ? card.dueDateUtc.slice(0, 10) : ""
  );
  const [labelIds, setLabelIds] = useState<string[]>(card.labelIds ?? []);
  const [comment, setComment] = useState("");
  const [newLabelName, setNewLabelName] = useState("");
  const [newLabelColor, setNewLabelColor] = useState(LABEL_COLORS[0]);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    setTitle(card.title);
    setDescription(card.description ?? "");
    setAssigneeId(card.assigneeId ?? "");
    setDueDate(card.dueDateUtc ? card.dueDateUtc.slice(0, 10) : "");
    setLabelIds(card.labelIds ?? []);
  }, [card]);

  const commentsQuery = useQuery({
    queryKey: ["comments", boardId, card.id],
    queryFn: () => getCardComments(boardId, card.id),
  });

  const saveMutation = useMutation({
    mutationFn: () =>
      updateCard(boardId, card.id, {
        title: title.trim(),
        description: description.trim() || null,
        assigneeId: assigneeId || null,
        dueDateUtc: dueDate ? new Date(`${dueDate}T12:00:00Z`).toISOString() : null,
        labelIds,
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["board", boardId] });
      onClose();
    },
    onError: () => setError("Could not save card."),
  });

  const deleteMutation = useMutation({
    mutationFn: () => deleteCard(boardId, card.id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["board", boardId] });
      onClose();
    },
    onError: () => setError("Could not delete card."),
  });

  const commentMutation = useMutation({
    mutationFn: (body: string) => addCardComment(boardId, card.id, body),
    onSuccess: () => {
      setComment("");
      queryClient.invalidateQueries({ queryKey: ["comments", boardId, card.id] });
    },
  });

  function toggleLabel(id: string) {
    setLabelIds((prev) => (prev.includes(id) ? prev.filter((x) => x !== id) : [...prev, id]));
  }

  async function handleCreateLabel(e: FormEvent) {
    e.preventDefault();
    if (!newLabelName.trim()) return;
    await onCreateLabel(newLabelName.trim(), newLabelColor);
    setNewLabelName("");
  }

  return (
    <div className="fixed inset-0 z-50 flex items-start justify-center bg-black/60 p-4 pt-16 overflow-y-auto">
      <div className="w-full max-w-2xl glass rounded-2xl p-6 space-y-5 relative">
        <button
          type="button"
          onClick={onClose}
          className="absolute top-4 right-4 text-slate-400 hover:text-white text-sm"
        >
          Close
        </button>

        <div>
          <label className="text-xs text-slate-400 uppercase tracking-wide">Title</label>
          <input
            className="tf-input mt-1"
            value={title}
            onChange={(e) => setTitle(e.target.value)}
          />
        </div>

        <div>
          <label className="text-xs text-slate-400 uppercase tracking-wide">Description</label>
          <textarea
            className="tf-input mt-1 min-h-[100px]"
            value={description}
            onChange={(e) => setDescription(e.target.value)}
            placeholder="Add a more detailed description…"
          />
        </div>

        <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
          <div>
            <label className="text-xs text-slate-400 uppercase tracking-wide">Assignee</label>
            <select
              className="tf-input mt-1"
              value={assigneeId}
              onChange={(e) => setAssigneeId(e.target.value)}
            >
              <option value="">Unassigned</option>
              {members.map((m) => (
                <option key={m.userId} value={m.userId}>
                  {m.displayName}
                </option>
              ))}
            </select>
          </div>
          <div>
            <label className="text-xs text-slate-400 uppercase tracking-wide">Due date</label>
            <input
              type="date"
              className="tf-input mt-1"
              value={dueDate}
              onChange={(e) => setDueDate(e.target.value)}
            />
          </div>
        </div>

        <div>
          <label className="text-xs text-slate-400 uppercase tracking-wide">Labels</label>
          <div className="flex flex-wrap gap-2 mt-2">
            {labels.map((l) => {
              const on = labelIds.includes(l.id);
              return (
                <button
                  key={l.id}
                  type="button"
                  onClick={() => toggleLabel(l.id)}
                  className={`text-xs px-2.5 py-1 rounded-full border transition-opacity ${
                    on ? "opacity-100 ring-2 ring-white/30" : "opacity-50"
                  }`}
                  style={{ backgroundColor: `${l.color}33`, borderColor: l.color, color: l.color }}
                >
                  {l.name}
                </button>
              );
            })}
          </div>
          <form onSubmit={handleCreateLabel} className="flex gap-2 mt-3">
            <input
              className="tf-input text-xs flex-1"
              placeholder="New label name"
              value={newLabelName}
              onChange={(e) => setNewLabelName(e.target.value)}
            />
            <select
              className="tf-input text-xs w-28"
              value={newLabelColor}
              onChange={(e) => setNewLabelColor(e.target.value)}
            >
              {LABEL_COLORS.map((c) => (
                <option key={c} value={c}>
                  {c}
                </option>
              ))}
            </select>
            <button type="submit" className="tf-btn tf-btn-ghost text-xs py-1.5 px-3">
              Add
            </button>
          </form>
        </div>

        <div>
          <label className="text-xs text-slate-400 uppercase tracking-wide">Comments</label>
          <div className="mt-2 space-y-2 max-h-40 overflow-y-auto">
            {(commentsQuery.data ?? []).map((c) => (
              <div key={c.id} className="rounded-lg bg-black/20 px-3 py-2 text-sm">
                <div className="text-xs text-slate-500 mb-0.5">
                  {c.authorName} · {new Date(c.createdAtUtc).toLocaleString()}
                </div>
                <div className="text-slate-200">{c.body}</div>
              </div>
            ))}
            {!commentsQuery.data?.length && (
              <p className="text-xs text-slate-500">No comments yet.</p>
            )}
          </div>
          <form
            className="flex gap-2 mt-2"
            onSubmit={(e) => {
              e.preventDefault();
              if (comment.trim()) commentMutation.mutate(comment.trim());
            }}
          >
            <input
              className="tf-input text-xs flex-1"
              placeholder="Write a comment…"
              value={comment}
              onChange={(e) => setComment(e.target.value)}
            />
            <button
              type="submit"
              className="tf-btn tf-btn-primary text-xs py-1.5 px-3"
              disabled={commentMutation.isPending}
            >
              Post
            </button>
          </form>
        </div>

        {error && <p className="text-sm text-rose-400">{error}</p>}

        <div className="flex justify-between pt-2">
          <button
            type="button"
            className="tf-btn text-xs py-1.5 px-3 text-rose-400 hover:bg-rose-500/10"
            onClick={() => {
              if (confirm("Delete this card?")) deleteMutation.mutate();
            }}
            disabled={deleteMutation.isPending}
          >
            Delete card
          </button>
          <div className="flex gap-2">
            <button type="button" className="tf-btn tf-btn-ghost text-xs py-1.5 px-3" onClick={onClose}>
              Cancel
            </button>
            <button
              type="button"
              className="tf-btn tf-btn-primary text-xs py-1.5 px-3"
              onClick={() => {
                if (!title.trim()) {
                  setError("Title is required.");
                  return;
                }
                setError(null);
                saveMutation.mutate();
              }}
              disabled={saveMutation.isPending}
            >
              Save
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}
