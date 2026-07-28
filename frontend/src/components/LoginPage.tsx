import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { useMutation } from "@tanstack/react-query";
import { login, register } from "../api/boards";
import { useAuthStore } from "../store/authStore";

export default function LoginPage() {
  const [isRegistering, setIsRegistering] = useState(false);
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [displayName, setDisplayName] = useState("");
  const [validationError, setValidationError] = useState<string | null>(null);

  const navigate = useNavigate();
  const setTokens = useAuthStore((s) => s.setTokens);

  const loginMutation = useMutation({
    mutationFn: () => login(email, password),
    onSuccess: (data) => {
      setTokens(data.accessToken, data.refreshToken);
      navigate("/boards");
    },
  });

  const registerMutation = useMutation({
    mutationFn: () => register(email, password, displayName || email.split("@")[0]),
    onSuccess: (data) => {
      setTokens(data.accessToken, data.refreshToken);
      navigate("/boards");
    },
  });

  const activeMutation = isRegistering ? registerMutation : loginMutation;

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    setValidationError(null);

    if (!email || !password) return;

    if (isRegistering) {
      if (password !== confirmPassword) {
        setValidationError("Passwords do not match.");
        return;
      }
      if (password.length < 6) {
        setValidationError("Password must be at least 6 characters long.");
        return;
      }
    }

    activeMutation.mutate();
  };

  const handleModeSwitch = (registering: boolean) => {
    setIsRegistering(registering);
    setValidationError(null);
    setConfirmPassword("");
    loginMutation.reset();
    registerMutation.reset();
  };

  return (
    <div className="auth-bg flex min-h-screen items-center justify-center p-4">
      <div className="glass w-full max-w-md rounded-3xl p-8 md:p-10 shadow-2xl relative z-10 fade-in border border-white/10">
        {/* Brand Header */}
        <div className="flex flex-col items-center text-center mb-8">
          <div className="w-14 h-14 rounded-2xl bg-gradient-to-tr from-indigo-600 to-indigo-400 p-0.5 shadow-lg shadow-indigo-500/25 mb-4">
            <div className="w-full h-full bg-[#0f1117] rounded-[14px] flex items-center justify-center text-indigo-400 font-extrabold text-2xl tracking-tighter">
              TF
            </div>
          </div>
          <h1 className="text-2xl md:text-3xl font-extrabold text-slate-100 tracking-tight">
            {isRegistering ? "Create your account" : "Welcome back"}
          </h1>
          <p className="mt-2 text-xs md:text-sm text-slate-400 max-w-xs">
            {isRegistering
              ? "Start managing your team projects and tasks in real-time with TaskFlow."
              : "Enter your credentials to access your Kanban workspaces."}
          </p>
        </div>

        <form className="space-y-4" onSubmit={handleSubmit}>
          {isRegistering && (
            <div className="fade-in">
              <label className="block text-[11px] font-bold text-slate-300 mb-1.5 uppercase tracking-wider">
                Full Name
              </label>
              <div className="relative">
                <input
                  type="text"
                  className="tf-input pl-10"
                  placeholder="Alex Morgan"
                  value={displayName}
                  onChange={(e) => setDisplayName(e.target.value)}
                />
                <span className="absolute left-3.5 top-3 text-slate-500 text-sm">👤</span>
              </div>
            </div>
          )}

          <div>
            <label className="block text-[11px] font-bold text-slate-300 mb-1.5 uppercase tracking-wider">
              Email Address
            </label>
            <div className="relative">
              <input
                type="email"
                required
                className="tf-input pl-10"
                placeholder="alex@example.com"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
              />
              <span className="absolute left-3.5 top-3 text-slate-500 text-sm"></span>
            </div>
          </div>

          <div>
            <label className="block text-[11px] font-bold text-slate-300 mb-1.5 uppercase tracking-wider">
              Password
            </label>
            <div className="relative">
              <input
                type="password"
                required
                className="tf-input pl-10"
                placeholder="••••••••"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
              />
              <span className="absolute left-3.5 top-3 text-slate-500 text-sm"></span>
            </div>
          </div>

          {isRegistering && (
            <div className="fade-in">
              <label className="block text-[11px] font-bold text-slate-300 mb-1.5 uppercase tracking-wider">
                Confirm Password
              </label>
              <div className="relative">
                <input
                  type="password"
                  required
                  className="tf-input pl-10"
                  placeholder="••••••••"
                  value={confirmPassword}
                  onChange={(e) => setConfirmPassword(e.target.value)}
                />
                <span className="absolute left-3.5 top-3 text-slate-500 text-sm">🛡️</span>
              </div>
            </div>
          )}

          {/* Validation Error Notice */}
          {validationError && (
            <div className="rounded-xl bg-rose-500/10 border border-rose-500/20 p-3.5 text-xs text-rose-400 font-medium flex items-center gap-2 fade-in">
              <span>⚠️</span>
              <span>{validationError}</span>
            </div>
          )}

          {/* Server API Error Notice */}
          {activeMutation.isError && !validationError && (
            <div className="rounded-xl bg-rose-500/10 border border-rose-500/20 p-3.5 text-xs text-rose-400 font-medium flex items-center gap-2 fade-in">
              <span>⚠️</span>
              <span>
                {isRegistering
                  ? "Registration failed. Email may already be registered."
                  : "Invalid email or password. Please try again."}
              </span>
            </div>
          )}

          <button
            type="submit"
            className="tf-btn tf-btn-primary w-full py-3.5 text-sm font-bold mt-3 shadow-indigo-600/30 hover:shadow-indigo-600/50"
            disabled={activeMutation.isPending}
          >
            {activeMutation.isPending ? (
              <div className="spinner" />
            ) : isRegistering ? (
              "Create Account"
            ) : (
              "Sign In"
            )}
          </button>
        </form>

        {/* Footer Link / Toggle at the bottom */}
        <div className="mt-8 pt-6 border-t border-white/10 text-center text-xs text-slate-400">
          {isRegistering ? (
            <p>
              Already have an account?{" "}
              <button
                type="button"
                onClick={() => handleModeSwitch(false)}
                className="text-indigo-400 font-semibold hover:text-indigo-300 transition-colors underline underline-offset-4 ml-1"
              >
                Sign in
              </button>
            </p>
          ) : (
            <p>
              Don't have an account?{" "}
              <button
                type="button"
                onClick={() => handleModeSwitch(true)}
                className="text-indigo-400 font-semibold hover:text-indigo-300 transition-colors underline underline-offset-4 ml-1"
              >
                Sign up
              </button>
            </p>
          )}
        </div>
      </div>
    </div>
  );
}
