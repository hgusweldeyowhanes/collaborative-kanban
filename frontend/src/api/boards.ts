import { AxiosError } from "axios";
import { apiClient } from "./client";
import type {
  ActivityDto,
  AuthResponse,
  BoardDetailDto,
  BoardMemberDto,
  BoardSummary,
  ChecklistDto,
  ChecklistItemDto,
  CommentDto,
  UpdateCardPayload,
} from "../types";

export function isConflictError(err: unknown): boolean {
  return err instanceof AxiosError && err.response?.status === 409;
}

export async function getBoards(): Promise<BoardSummary[]> {
  const { data } = await apiClient.get<BoardSummary[]>("/api/boards");
  return data;
}

export async function getBoardDetail(boardId: string): Promise<BoardDetailDto> {
  const { data } = await apiClient.get<BoardDetailDto>(`/api/boards/${boardId}`);
  return data;
}

export async function getBoardActivities(boardId: string): Promise<ActivityDto[]> {
  const { data } = await apiClient.get<ActivityDto[]>(`/api/boards/${boardId}/activities`);
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
  newOrder: number,
  rowVersion?: string | null
): Promise<void> {
  await apiClient.post("/api/boards/cards/move", {
    boardId,
    cardId,
    targetListId,
    newOrder,
    rowVersion: rowVersion ?? null,
  });
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

export async function getCardChecklists(boardId: string, cardId: string): Promise<ChecklistDto[]> {
  const { data } = await apiClient.get<ChecklistDto[]>(
    `/api/boards/${boardId}/cards/${cardId}/checklists`
  );
  return data;
}

export async function createChecklist(
  boardId: string,
  cardId: string,
  title: string
): Promise<ChecklistDto> {
  const { data } = await apiClient.post<ChecklistDto>(
    `/api/boards/${boardId}/cards/${cardId}/checklists`,
    { title }
  );
  return data;
}

export async function addChecklistItem(
  boardId: string,
  cardId: string,
  checklistId: string,
  title: string
): Promise<ChecklistItemDto> {
  const { data } = await apiClient.post<ChecklistItemDto>(
    `/api/boards/${boardId}/cards/${cardId}/checklists/${checklistId}/items`,
    { title }
  );
  return data;
}

export async function toggleChecklistItem(
  boardId: string,
  cardId: string,
  itemId: string
): Promise<ChecklistItemDto> {
  const { data } = await apiClient.post<ChecklistItemDto>(
    `/api/boards/${boardId}/cards/${cardId}/checklist-items/${itemId}/toggle`
  );
  return data;
}

export async function deleteChecklist(
  boardId: string,
  cardId: string,
  checklistId: string
): Promise<void> {
  await apiClient.delete(`/api/boards/${boardId}/cards/${cardId}/checklists/${checklistId}`);
}

export async function deleteChecklistItem(
  boardId: string,
  cardId: string,
  itemId: string
): Promise<void> {
  await apiClient.delete(`/api/boards/${boardId}/cards/${cardId}/checklist-items/${itemId}`);
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
