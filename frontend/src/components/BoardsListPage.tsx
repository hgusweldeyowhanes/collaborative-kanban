import { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { createBoard, getBoards } from "../api/boards";
import { useAuthStore } from "../store/authStore";

export default function BoardsListPage() {
  const [title, setTitle] = useState("");
  const [description, setDescription] = useState("");
  const [isCreating, setIsCreating] = useState(false);

  const queryClient = useQueryClient();
  const navigate = useNavigate();
  const clearAuth = useAuthStore((s) => s.clear);

  const { data: boards, isLoading, isError } = useQuery({
    queryKey: ["boards"],
    queryFn: getBoards,
  });

  const createMutation = useMutation({
    mutationFn: () => createBoard(title, description),
    onSuccess: (newBoardId) => {
      setTitle("");
      setDescription("");
      setIsCreating(false);
      queryClient.invalidateQueries({ queryKey: ["boards"] });
      if (newBoardId) {
        navigate(`/boards/${newBoardId}`);
      }
    },
  });

  const handleLogout = () => {
    clearAuth();
    navigate("/login");
  };

  return (
    <div className="min-h-screen bg-[#0f1117] text-slate-100 flex flex-col">
      {/* Top Navbar */}
      <header className="navbar flex items-center justify-between">
        <div className="flex items-center gap-3">
          <div className="w-8 h-8 rounded-lg bg-indigo-600/20 border border-indigo-500/30 flex items-center justify-center text-indigo-400 font-bold text-sm">
            TF
          </div>
          <span className="font-bold text-lg tracking-tight gradient-text">TaskFlow</span>
        </div>

        <div className="flex items-center gap-4">
          <button
            onClick={handleLogout}
            className="tf-btn tf-btn-ghost text-xs py-1.5 px-3"
          >
            Log Out
          </button>
        </div>
      </header>

      {/* Main Content Container */}
      <main className="flex-1 max-w-5xl w-full mx-auto p-6 md:p-8 fade-in">
        <div className="flex items-center justify-between mb-8">
          <div>
            <h1 className="text-2xl font-bold tracking-tight text-slate-100">Your Workspaces</h1>
            <p className="text-sm text-slate-400 mt-1">Select a board to manage tasks and collaborate in real-time.</p>
          </div>

          <button
            onClick={() => setIsCreating(!isCreating)}
            className="tf-btn tf-btn-primary"
          >
            {isCreating ? "Cancel" : "+ Create Board"}
          </button>
        </div>

        {/* Create Board Modal/Inline Form */}
        {isCreating && (
          <div className="glass rounded-2xl p-6 mb-8 border border-indigo-500/20 shadow-xl fade-in">
            <h3 className="text-base font-semibold text-slate-200 mb-4">Create New Board</h3>
            <form
              className="space-y-4"
              onSubmit={(e) => {
                e.preventDefault();
                if (title.trim()) createMutation.mutate();
              }}
            >
              <div>
                <label className="block text-xs font-semibold text-slate-400 mb-1.5 uppercase tracking-wider">
                  Board Title *
                </label>
                <input
                  type="text"
                  required
                  className="tf-input"
                  placeholder="e.g. Q3 Roadmap, Marketing Campaign"
                  value={title}
                  onChange={(e) => setTitle(e.target.value)}
                />
              </div>

              <div>
                <label className="block text-xs font-semibold text-slate-400 mb-1.5 uppercase tracking-wider">
                  Description (Optional)
                </label>
                <input
                  type="text"
                  className="tf-input"
                  placeholder="Short summary of this board's goals..."
                  value={description}
                  onChange={(e) => setDescription(e.target.value)}
                />
              </div>

              <div className="flex justify-end gap-3 pt-2">
                <button
                  type="button"
                  onClick={() => setIsCreating(false)}
                  className="tf-btn tf-btn-ghost"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  className="tf-btn tf-btn-primary"
                  disabled={createMutation.isPending}
                >
                  {createMutation.isPending ? <div className="spinner" /> : "Save Board"}
                </button>
              </div>
            </form>
          </div>
        )}

        {/* Loading State */}
        {isLoading && (
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
            {[1, 2, 3].map((i) => (
              <div key={i} className="h-32 rounded-xl bg-slate-800/40 animate-pulse border border-white/5" />
            ))}
          </div>
        )}

        {/* Error State */}
        {isError && (
          <div className="p-4 rounded-xl bg-rose-500/10 border border-rose-500/20 text-rose-400 text-sm">
            Failed to load boards. Please check your network or try re-logging in.
          </div>
        )}

        {/* Empty State */}
        {!isLoading && boards?.length === 0 && !isCreating && (
          <div className="glass rounded-2xl p-12 text-center border border-dashed border-slate-700">
            <div className="w-12 h-12 rounded-2xl bg-indigo-500/10 border border-indigo-500/20 flex items-center justify-center text-indigo-400 mx-auto mb-4 text-xl">
              📋
            </div>
            <h3 className="text-lg font-semibold text-slate-200">No boards created yet</h3>
            <p className="text-sm text-slate-400 mt-1 max-w-sm mx-auto">
              Get started by creating your first board to manage tasks, sprints, or personal goals.
            </p>
            <button
              onClick={() => setIsCreating(true)}
              className="tf-btn tf-btn-primary mt-6"
            >
              + Create Your First Board
            </button>
          </div>
        )}

        {/* Boards Grid */}
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-5">
          {boards?.map((board) => (
            <Link
              key={board.id}
              to={`/boards/${board.id}`}
              className="board-card group"
            >
              <div className="flex items-start justify-between mb-3">
                <h2 className="font-semibold text-slate-100 text-lg group-hover:text-indigo-400 transition-colors">
                  {board.title}
                </h2>
                <span className="badge text-[10px]">Active</span>
              </div>
              <p className="text-xs text-slate-400 line-clamp-2 min-h-[32px]">
                {board.description || "No description provided."}
              </p>
              <div className="mt-4 pt-3 border-t border-white/5 flex items-center justify-between text-[11px] text-slate-500">
                <span>Created {new Date(board.createdAtUtc).toLocaleDateString()}</span>
                <span className="text-indigo-400 group-hover:translate-x-1 transition-transform">
                  Open Board →
                </span>
              </div>
            </Link>
          ))}
        </div>
      </main>
    </div>
  );
}
