import { useEffect, useRef } from "react";
import * as signalR from "@microsoft/signalr";
import { useAuthStore } from "../store/authStore";
import { API_BASE_URL } from "../api/client";
import type { CardMovedEvent } from "../types";

interface UseSignalROptions {
  boardId: string | undefined;
  onCardMoved: (event: CardMovedEvent) => void;
}

// Opens one SignalR connection per mounted board view, joins the
// board-scoped group on the server, and tears down cleanly on unmount
// or when the user navigates to a different board.
export function useSignalR({ boardId, onCardMoved }: UseSignalROptions) {
  const connectionRef = useRef<signalR.HubConnection | null>(null);

  useEffect(() => {
    if (!boardId) return;

    const accessToken = useAuthStore.getState().accessToken;

    const connection = new signalR.HubConnectionBuilder()
      .withUrl(`${API_BASE_URL}/hubs/board`, {
        accessTokenFactory: () => accessToken ?? "",
      })
      .withAutomaticReconnect()
      .build();

    connection.on("CardMoved", onCardMoved);

    connection
      .start()
      .then(() => connection.invoke("JoinBoard", boardId))
      .catch((err) => console.error("SignalR connection failed:", err));

    connectionRef.current = connection;

    return () => {
      connection.invoke("LeaveBoard", boardId).catch(() => {});
      connection.stop();
      connectionRef.current = null;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [boardId]);
}
