import { Navigate, Route, Routes } from "react-router-dom";
import { useAuthStore } from "./store/authStore";
import LoginPage from "./components/LoginPage";
import BoardsListPage from "./components/BoardsListPage";
import BoardViewPage from "./components/BoardViewPage";

function RequireAuth({ children }: { children: React.ReactNode }) {
  const refreshToken = useAuthStore((s) => s.refreshToken);
  if (!refreshToken) return <Navigate to="/login" replace />;
  return <>{children}</>;
}

export default function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route
        path="/boards"
        element={
          <RequireAuth>
            <BoardsListPage />
          </RequireAuth>
        }
      />
      <Route
        path="/boards/:boardId"
        element={
          <RequireAuth>
            <BoardViewPage />
          </RequireAuth>
        }
      />
      <Route path="*" element={<Navigate to="/boards" replace />} />
    </Routes>
  );
}
