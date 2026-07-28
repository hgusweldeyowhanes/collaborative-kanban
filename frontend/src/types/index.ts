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

export interface CardDto {
  id: string;
  title: string;
  description: string | null;
  order: number;
  assigneeId: string | null;
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
}
