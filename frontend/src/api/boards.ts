import { apiClient } from "./client";
import type {
  AuthResponse,
  BoardDetailDto,
  BoardMemberDto,
  BoardSummary,
  CommentDto,
  UpdateCardPayload,
} from "../types";

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
  const { data } = await apiClient.post<string>(
    `/api/boards/${boardId}/lists/${listId}/cards`,
    { title }
  );
  return data;
}

export async function updateCard(
  boardId: string,
  cardId: string,
  payload: UpdateCardPayload
): Promise<void> {
  await apiClient.put(`/api/boards/${boardId}/cards/${cardId}`, payload);
}

export async function deleteCard(boardId: string, cardId: string): Promise<void> {
  await apiClient.delete(`/api/boards/${boardId}/cards/${cardId}`);
}

export async function moveCard(
  boardId: string,
  cardId: string,
  targetListId: string,
  newOrder: number
): Promise<void> {
  await apiClient.post("/api/boards/cards/move", { boardId, cardId, targetListId, newOrder });
}

export async function getBoardMembers(boardId: string): Promise<BoardMemberDto[]> {
  const { data } = await apiClient.get<BoardMemberDto[]>(`/api/boards/${boardId}/members`);
  return data;
}

export async function inviteMember(
  boardId: string,
  email: string,
  role: number
): Promise<string> {
  const { data } = await apiClient.post<string>(`/api/boards/${boardId}/members`, { email, role });
  return data;
}

export async function createLabel(
  boardId: string,
  name: string,
  color: string
): Promise<string> {
  const { data } = await apiClient.post<string>(`/api/boards/${boardId}/labels`, { name, color });
  return data;
}

export async function getCardComments(boardId: string, cardId: string): Promise<CommentDto[]> {
  const { data } = await apiClient.get<CommentDto[]>(
    `/api/boards/${boardId}/cards/${cardId}/comments`
  );
  return data;
}

export async function addCardComment(
  boardId: string,
  cardId: string,
  body: string
): Promise<CommentDto> {
  const { data } = await apiClient.post<CommentDto>(
    `/api/boards/${boardId}/cards/${cardId}/comments`,
    { body }
  );
  return data;
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
