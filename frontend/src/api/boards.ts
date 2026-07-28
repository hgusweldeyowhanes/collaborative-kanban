import { apiClient } from "./client";
import type { AuthResponse, BoardDetailDto, BoardSummary } from "../types";

export async function getBoards(): Promise<BoardSummary[]> {
  const { data } = await apiClient.get<BoardSummary[]>("/api/boards");
  return data;
}

export async function getBoardDetail(boardId: string): Promise<BoardDetailDto> {
  const { data } = await apiClient.get<BoardDetailDto>(`/api/boards/${boardId}`);
  return data;
}

export async function createBoard(title: string, description?: string): Promise<string> {
  const { data } = await apiClient.post<string>("/api/boards", { title, description });
  return data;
}

export async function createList(boardId: string, title: string): Promise<string> {
  const { data } = await apiClient.post<string>(`/api/boards/${boardId}/lists`, { title });
  return data;
}

export async function createCard(boardId: string, listId: string, title: string): Promise<string> {
  const { data } = await apiClient.post<string>(`/api/boards/${boardId}/lists/${listId}/cards`, { title });
  return data;
}

export async function moveCard(
  boardId: string,
  cardId: string,
  targetListId: string,
  newOrder: number
): Promise<void> {
  await apiClient.post("/api/boards/cards/move", { boardId, cardId, targetListId, newOrder });
}

export async function login(email: string, password: string): Promise<AuthResponse> {
  const { data } = await apiClient.post<AuthResponse>("/api/auth/login", { email, password });
  return data;
}

export async function register(
  email: string,
  password: string,
  displayName: string
): Promise<AuthResponse> {
  const { data } = await apiClient.post<AuthResponse>("/api/auth/register", {
    email,
    password,
    displayName,
  });
  return data;
}
