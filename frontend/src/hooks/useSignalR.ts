import { useEffect, useRef } from "react";
import * as signalR from "@microsoft/signalr";
import { useAuthStore } from "../store/authStore";
import { API_BASE_URL } from "../api/client";
import type { BoardChangedEvent, CardMovedEvent } from "../types";

interface UseSignalROptions {
  boardId: string | undefined;
  onCardMoved: (event: CardMovedEvent) => void;
  onBoardChanged?: (event: BoardChangedEvent) => void;
}

export function useSignalR({ boardId, onCardMoved, onBoardChanged }: UseSignalROptions) {
  const onCardMovedRef = useRef(onCardMoved);
  const onBoardChangedRef = useRef(onBoardChanged);
  onCardMovedRef.current = onCardMoved;
  onBoardChangedRef.current = onBoardChanged;

  useEffect(() => {
    if (!boardId) return;

    const accessToken = useAuthStore.getState().accessToken;

    const connection = new signalR.HubConnectionBuilder()
      .withUrl(`${API_BASE_URL}/hubs/board`, {
        accessTokenFactory: () => accessToken ?? "",
      })
      .withAutomaticReconnect()
      .build();

    connection.on("CardMoved", (event: CardMovedEvent) => onCardMovedRef.current(event));
    connection.on("BoardChanged", (event: BoardChangedEvent) => {
      onBoardChangedRef.current?.(event);
    });

    connection
      .start()
      .then(() => connection.invoke("JoinBoard", boardId))
      .catch((err) => console.error("SignalR connection failed:", err));

    return () => {
      connection.invoke("LeaveBoard", boardId).catch(() => {});
      connection.stop();
    };
  }, [boardId]);
}
