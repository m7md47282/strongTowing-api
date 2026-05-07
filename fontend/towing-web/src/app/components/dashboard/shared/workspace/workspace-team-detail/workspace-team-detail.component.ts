import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import { finalize } from 'rxjs/operators';
import {
  WorkspaceService,
  WorkspaceTeam,
  WorkspaceTeamMember,
  TaskBoardSummary,
  CompanyEmployee,
} from '../../../../../services/workspace.service';
import { workspaceShellPrefix } from '../workspace.routes-helper';

@Component({
  selector: 'app-workspace-team-detail',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './workspace-team-detail.component.html',
  styleUrl: './workspace-team-detail.component.scss',
})
export class WorkspaceTeamDetailComponent implements OnInit {
  teamId!: number;
  team: WorkspaceTeam | null = null;
  members: WorkspaceTeamMember[] = [];
  boards: TaskBoardSummary[] = [];
  roster: CompanyEmployee[] = [];

  loading = true;
  error: string | null = null;

  memberUserId = '';
  newBoardName = '';
  newBoardDescription = '';
  addingMember = false;
  creatingBoard = false;

  editTeamName = '';
  editTeamDescription = '';
  savingTeam = false;

  constructor(
    private route: ActivatedRoute,
    public router: Router,
    private workspace: WorkspaceService
  ) {}

  shell(): string {
    return workspaceShellPrefix(this.router);
  }

  ngOnInit(): void {
    this.route.paramMap.subscribe((pm) => {
      const id = pm.get('teamId');
      this.teamId = id ? +id : NaN;
      if (!Number.isFinite(this.teamId)) {
        this.router.navigate([this.shell(), 'workspace']);
        return;
      }
      this.load();
    });
  }

  load(): void {
    this.loading = true;
    this.error = null;
    forkJoin({
      team: this.workspace.getTeam(this.teamId),
      members: this.workspace.getTeamMembers(this.teamId),
      boards: this.workspace.getBoards(this.teamId),
      roster: this.workspace.getEmployees(),
    }).subscribe({
      next: ({ team, members, boards, roster }) => {
        this.team = team;
        this.members = members;
        this.boards = boards;
        this.roster = roster;
        this.editTeamName = team.name;
        this.editTeamDescription = team.description || '';
      },
      error: () => (this.error = 'Could not load team'),
      complete: () => (this.loading = false),
    });
  }

  rosterNotOnTeam(): CompanyEmployee[] {
    const ids = new Set(this.members.map((m) => m.userId));
    return this.roster.filter((e) => !ids.has(e.userId));
  }

  saveTeam(): void {
    const name = this.editTeamName.trim();
    if (!name || !this.team) return;
    this.savingTeam = true;
    this.workspace
      .updateTeam(this.teamId, {
        name,
        description: this.editTeamDescription.trim() || null,
      })
      .pipe(finalize(() => (this.savingTeam = false)))
      .subscribe({
        next: () => this.load(),
        error: (e) => (this.error = e?.error?.message || 'Could not update team'),
      });
  }

  addMember(): void {
    if (!this.memberUserId) return;
    this.addingMember = true;
    this.workspace
      .addTeamMember(this.teamId, this.memberUserId)
      .pipe(finalize(() => (this.addingMember = false)))
      .subscribe({
        next: () => {
          this.memberUserId = '';
          this.reloadMembersBoards();
        },
        error: (e) => (this.error = e?.error?.message || 'Could not add member'),
      });
  }

  removeMember(userId: string): void {
    if (!confirm('Remove this member from the team?')) return;
    this.workspace.removeTeamMember(this.teamId, userId).subscribe({
      next: () => this.reloadMembersBoards(),
      error: () => (this.error = 'Could not remove member'),
    });
  }

  createBoard(): void {
    const name = this.newBoardName.trim();
    if (!name) return;
    this.creatingBoard = true;
    this.workspace
      .createBoard(this.teamId, {
        name,
        description: this.newBoardDescription.trim() || null,
      })
      .pipe(finalize(() => (this.creatingBoard = false)))
      .subscribe({
        next: (b) => {
          this.newBoardName = '';
          this.newBoardDescription = '';
          this.router.navigate([this.shell(), 'workspace', 'board', b.id]);
        },
        error: (e) => (this.error = e?.error?.message || 'Could not create board'),
      });
  }

  deleteBoard(board: TaskBoardSummary): void {
    if (!confirm(`Delete board "${board.name}" and all tickets?`)) return;
    this.workspace.deleteBoard(board.id).subscribe({
      next: () => this.workspace.getBoards(this.teamId).subscribe({ next: (r) => (this.boards = r) }),
      error: () => (this.error = 'Could not delete board'),
    });
  }

  private reloadMembersBoards(): void {
    forkJoin({
      members: this.workspace.getTeamMembers(this.teamId),
      boards: this.workspace.getBoards(this.teamId),
      team: this.workspace.getTeam(this.teamId),
    }).subscribe({
      next: ({ members, boards, team }) => {
        this.members = members;
        this.boards = boards;
        this.team = team;
      },
    });
  }
}
