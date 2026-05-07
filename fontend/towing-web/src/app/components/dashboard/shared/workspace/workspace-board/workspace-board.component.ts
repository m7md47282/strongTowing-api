import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import {
  DragDropModule,
  CdkDragDrop,
  moveItemInArray,
  transferArrayItem,
} from '@angular/cdk/drag-drop';
import { forkJoin } from 'rxjs';
import { finalize } from 'rxjs/operators';
import {
  WorkspaceService,
  TaskBoardDetail,
  TaskTicket,
  TaskBoardMember,
  WorkspaceTeamMember,
} from '../../../../../services/workspace.service';
import { workspaceShellPrefix } from '../workspace.routes-helper';

@Component({
  selector: 'app-workspace-board',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, DragDropModule],
  templateUrl: './workspace-board.component.html',
  styleUrl: './workspace-board.component.scss',
})
export class WorkspaceBoardComponent implements OnInit {
  boardId!: number;
  board: TaskBoardDetail | null = null;
  boardMembers: TaskBoardMember[] = [];
  teamMembers: WorkspaceTeamMember[] = [];

  loading = true;
  error: string | null = null;

  newTitle = '';
  newDescription = '';
  newPriority = 1;
  newAssigneeId = '';
  createColumnId: number | null = null;
  creatingTicket = false;

  boardMemberUserId = '';

  priorityLabels = ['Low', 'Normal', 'High', 'Urgent'];

  constructor(
    private route: ActivatedRoute,
    public router: Router,
    private workspace: WorkspaceService
  ) {}

  shell(): string {
    return workspaceShellPrefix(this.router);
  }

  get connectedListIds(): string[] {
    return this.board?.columns.map((c) => this.columnListId(c.id)) ?? [];
  }

  columnListId(columnId: number): string {
    return `ws-col-${columnId}`;
  }

  ngOnInit(): void {
    this.route.paramMap.subscribe((pm) => {
      const id = pm.get('boardId');
      this.boardId = id ? +id : NaN;
      if (!Number.isFinite(this.boardId)) {
        this.router.navigate([this.shell(), 'workspace']);
        return;
      }
      this.loadBoard();
    });
  }

  loadBoard(): void {
    this.loading = true;
    this.error = null;
    this.workspace.getBoard(this.boardId).subscribe({
      next: (b) => {
        this.board = b;
        if (!this.createColumnId && b.columns.length) {
          this.createColumnId = b.columns[0].id;
        }
        this.loadMembers();
      },
      error: () => {
        this.error = 'Could not load board (check you are on the board or are a manager)';
        this.loading = false;
      },
    });
  }

  loadMembers(): void {
    if (!this.board) return;
    forkJoin({
      boardMembers: this.workspace.getBoardMembers(this.boardId),
      teamMembers: this.workspace.getTeamMembers(this.board.teamId),
    }).subscribe({
      next: ({ boardMembers, teamMembers }) => {
        this.boardMembers = boardMembers;
        this.teamMembers = teamMembers;
      },
      complete: () => (this.loading = false),
    });
  }

  teamMembersNotOnBoard(): WorkspaceTeamMember[] {
    const ids = new Set(this.boardMembers.map((m) => m.userId));
    return this.teamMembers.filter((m) => !ids.has(m.userId));
  }

  addBoardMember(): void {
    if (!this.boardMemberUserId) return;
    this.workspace.addBoardMember(this.boardId, this.boardMemberUserId).subscribe({
      next: () => {
        this.boardMemberUserId = '';
        this.workspace.getBoardMembers(this.boardId).subscribe({ next: (r) => (this.boardMembers = r) });
      },
      error: (e) => (this.error = e?.error?.message || 'Could not add to board'),
    });
  }

  removeBoardMember(userId: string): void {
    if (!confirm('Remove this person from the board?')) return;
    this.workspace.removeBoardMember(this.boardId, userId).subscribe({
      next: () =>
        this.workspace.getBoardMembers(this.boardId).subscribe({ next: (r) => (this.boardMembers = r) }),
      error: () => (this.error = 'Could not remove'),
    });
  }

  createTicket(): void {
    const title = this.newTitle.trim();
    if (!title || !this.createColumnId) return;
    this.creatingTicket = true;
    this.workspace
      .createTicket(this.boardId, {
        title,
        description: this.newDescription.trim() || null,
        columnId: this.createColumnId,
        assigneeUserId: this.newAssigneeId || null,
        priority: this.newPriority,
      })
      .pipe(finalize(() => (this.creatingTicket = false)))
      .subscribe({
        next: () => {
          this.newTitle = '';
          this.newDescription = '';
          this.newAssigneeId = '';
          this.newPriority = 1;
          this.reloadTicketsOnly();
        },
        error: (e) => (this.error = e?.error?.message || 'Could not create ticket'),
      });
  }

  drop(event: CdkDragDrop<TaskTicket[]>, targetColumnId: number): void {
    if (!this.board) return;
    if (event.previousContainer === event.container) {
      moveItemInArray(event.container.data, event.previousIndex, event.currentIndex);
      const t = event.container.data[event.currentIndex];
      this.workspace.updateTicket(t.id, { sortOrder: event.currentIndex }).subscribe({
        error: () => this.reloadTicketsOnly(),
      });
      return;
    }
    transferArrayItem(
      event.previousContainer.data,
      event.container.data,
      event.previousIndex,
      event.currentIndex
    );
    const t = event.container.data[event.currentIndex];
    this.workspace
      .updateTicket(t.id, { columnId: targetColumnId, sortOrder: event.currentIndex })
      .subscribe({
        error: () => this.reloadTicketsOnly(),
      });
  }

  deleteTicket(t: TaskTicket): void {
    if (!confirm('Delete this ticket?')) return;
    this.workspace.deleteTicket(t.id).subscribe({
      next: () => this.reloadTicketsOnly(),
      error: () => (this.error = 'Could not delete'),
    });
  }

  assigneesForSelect(): TaskBoardMember[] {
    return this.boardMembers;
  }

  private reloadTicketsOnly(): void {
    this.workspace.getBoard(this.boardId).subscribe({
      next: (b) => (this.board = b),
    });
  }
}
