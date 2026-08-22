export interface BoardSummary {
  id: string;
  title: string;
  description: string | null;
  createdAtUtc: string;
}

export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
}

export interface CardMovedEvent {
  boardId: string;
  cardId: string;
  targetListId: string;
  newOrder: number;
}

export interface BoardChangedEvent {
  boardId: string;
  changeType: string;
}

export type BoardRole = "Viewer" | "Editor" | "Admin" | 0 | 1 | 2;

export interface LabelDto {
  id: string;
  name: string;
  color: string;
}

export interface MemberSummaryDto {
  userId: string;
  displayName: string;
  role: BoardRole;
}

export interface BoardMemberDto {
  userId: string;
  email: string;
  displayName: string;
  role: BoardRole;
}

export interface CardDto {
  id: string;
  title: string;
  description: string | null;
  order: number;
  assigneeId: string | null;
  assigneeName: string | null;
  dueDateUtc: string | null;
  labelIds: string[];
}

export interface BoardListDto {
  id: string;
  title: string;
  order: number;
  cards: CardDto[];
}

export interface BoardDetailDto {
  id: string;
  title: string;
  description: string | null;
  lists: BoardListDto[];
  labels: LabelDto[];
  members: MemberSummaryDto[];
}

export interface CommentDto {
  id: string;
  authorId: string;
  authorName: string;
  body: string;
  createdAtUtc: string;
}

export interface UpdateCardPayload {
  title: string;
  description?: string | null;
  assigneeId?: string | null;
  dueDateUtc?: string | null;
  labelIds?: string[];
}
