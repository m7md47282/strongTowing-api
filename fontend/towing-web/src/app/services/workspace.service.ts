import { Injectable } from '@angular/core';
import { ApiService } from './api.service';
import { Observable } from 'rxjs';

export interface WorkspaceUserOption {
  id: string;
  email: string;
  fullName: string;
  phoneNumber?: string | null;
  role: string;
  roleId?: string | number;
}

export interface CompanyEmployee {
  id: number;
  userId: string;
  email: string;
  fullName: string;
  phoneNumber?: string | null;
  role: string;
  jobTitle?: string | null;
  department?: string | null;
  createdAt: string;
}

export interface WorkspaceTeam {
  id: number;
  name: string;
  description?: string | null;
  createdAt: string;
  memberCount: number;
  boardCount: number;
}

export interface WorkspaceTeamMember {
  userId: string;
  email: string;
  fullName: string;
  role: string;
  joinedAt: string;
}

export interface TaskBoardSummary {
  id: number;
  name: string;
  description?: string | null;
  createdAt: string;
  memberCount: number;
}

export interface TaskTicket {
  id: number;
  boardId: number;
  columnId: number;
  title: string;
  description?: string | null;
  priority: number;
  sortOrder: number;
  assigneeUserId?: string | null;
  assigneeName?: string | null;
  createdByUserId?: string | null;
  createdByName?: string | null;
  createdAt: string;
  updatedAt?: string | null;
}

export interface TaskBoardColumn {
  id: number;
  title: string;
  sortOrder: number;
  tickets: TaskTicket[];
}

export interface TaskBoardDetail {
  id: number;
  teamId: number;
  teamName: string;
  name: string;
  description?: string | null;
  createdAt: string;
  columns: TaskBoardColumn[];
}

export interface TaskBoardMember {
  userId: string;
  email: string;
  fullName: string;
  role: string;
  joinedAt: string;
}

@Injectable({ providedIn: 'root' })
export class WorkspaceService {
  constructor(private api: ApiService) {}

  getEligibleUsers(search?: string): Observable<WorkspaceUserOption[]> {
    const q = search?.trim() ? `?search=${encodeURIComponent(search.trim())}&take=300` : '?take=300';
    return this.api.get<WorkspaceUserOption[]>(`workspace/eligible-users${q}`);
  }

  getEmployees(): Observable<CompanyEmployee[]> {
    return this.api.get<CompanyEmployee[]>('workspace/employees');
  }

  addEmployee(body: { userId: string; jobTitle?: string | null; department?: string | null }): Observable<CompanyEmployee> {
    return this.api.post<CompanyEmployee>('workspace/employees', body);
  }

  removeEmployee(userId: string): Observable<void> {
    return this.api.delete<void>(`workspace/employees/${userId}`);
  }

  getTeams(): Observable<WorkspaceTeam[]> {
    return this.api.get<WorkspaceTeam[]>('workspace/teams');
  }

  getTeam(teamId: number): Observable<WorkspaceTeam> {
    return this.api.get<WorkspaceTeam>(`workspace/teams/${teamId}`);
  }

  createTeam(body: { name: string; description?: string | null }): Observable<WorkspaceTeam> {
    return this.api.post<WorkspaceTeam>('workspace/teams', body);
  }

  updateTeam(teamId: number, body: { name: string; description?: string | null }): Observable<void> {
    return this.api.put<void>(`workspace/teams/${teamId}`, body);
  }

  deleteTeam(teamId: number): Observable<void> {
    return this.api.delete<void>(`workspace/teams/${teamId}`);
  }

  getTeamMembers(teamId: number): Observable<WorkspaceTeamMember[]> {
    return this.api.get<WorkspaceTeamMember[]>(`workspace/teams/${teamId}/members`);
  }

  addTeamMember(teamId: number, userId: string): Observable<void> {
    return this.api.post<void>(`workspace/teams/${teamId}/members`, { userId });
  }

  removeTeamMember(teamId: number, userId: string): Observable<void> {
    return this.api.delete<void>(`workspace/teams/${teamId}/members/${userId}`);
  }

  getBoards(teamId: number): Observable<TaskBoardSummary[]> {
    return this.api.get<TaskBoardSummary[]>(`workspace/teams/${teamId}/boards`);
  }

  createBoard(teamId: number, body: { name: string; description?: string | null }): Observable<TaskBoardSummary> {
    return this.api.post<TaskBoardSummary>(`workspace/teams/${teamId}/boards`, body);
  }

  getBoard(boardId: number): Observable<TaskBoardDetail> {
    return this.api.get<TaskBoardDetail>(`workspace/boards/${boardId}`);
  }

  deleteBoard(boardId: number): Observable<void> {
    return this.api.delete<void>(`workspace/boards/${boardId}`);
  }

  getBoardMembers(boardId: number): Observable<TaskBoardMember[]> {
    return this.api.get<TaskBoardMember[]>(`workspace/boards/${boardId}/members`);
  }

  addBoardMember(boardId: number, userId: string): Observable<void> {
    return this.api.post<void>(`workspace/boards/${boardId}/members`, { userId });
  }

  removeBoardMember(boardId: number, userId: string): Observable<void> {
    return this.api.delete<void>(`workspace/boards/${boardId}/members/${userId}`);
  }

  createTicket(
    boardId: number,
    body: {
      title: string;
      description?: string | null;
      columnId?: number | null;
      assigneeUserId?: string | null;
      priority?: number;
    }
  ): Observable<TaskTicket> {
    return this.api.post<TaskTicket>(`workspace/boards/${boardId}/tickets`, body);
  }

  updateTicket(
    ticketId: number,
    body: {
      title?: string;
      description?: string | null;
      columnId?: number | null;
      sortOrder?: number | null;
      assigneeUserId?: string | null;
      priority?: number | null;
    }
  ): Observable<TaskTicket> {
    return this.api.patch<TaskTicket>(`workspace/tickets/${ticketId}`, body);
  }

  deleteTicket(ticketId: number): Observable<void> {
    return this.api.delete<void>(`workspace/tickets/${ticketId}`);
  }
}
