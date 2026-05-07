import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs/operators';
import { forkJoin } from 'rxjs';
import {
  WorkspaceService,
  CompanyEmployee,
  WorkspaceTeam,
  WorkspaceUserOption,
} from '../../../../../services/workspace.service';
import { workspaceShellPrefix } from '../workspace.routes-helper';

@Component({
  selector: 'app-workspace-hub',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './workspace-hub.component.html',
  styleUrl: './workspace-hub.component.scss',
})
export class WorkspaceHubComponent implements OnInit {
  tab: 'employees' | 'teams' = 'employees';
  loading = false;
  error: string | null = null;

  employees: CompanyEmployee[] = [];
  teams: WorkspaceTeam[] = [];

  eligiblePick: WorkspaceUserOption[] = [];
  eligibleSearch = '';
  addUserId = '';
  addJobTitle = '';
  addDepartment = '';
  addingEmployee = false;

  newTeamName = '';
  newTeamDescription = '';
  creatingTeam = false;

  constructor(
    private workspace: WorkspaceService,
    public router: Router
  ) {}

  shell(): string {
    return workspaceShellPrefix(this.router);
  }

  ngOnInit(): void {
    this.refreshAll();
    this.loadEligible();
  }

  refreshAll(): void {
    this.loading = true;
    this.error = null;
    forkJoin({
      employees: this.workspace.getEmployees(),
      teams: this.workspace.getTeams(),
    }).subscribe({
      next: ({ employees, teams }) => {
        this.employees = employees;
        this.teams = teams;
      },
      error: () => (this.error = 'Could not load workspace data'),
      complete: () => (this.loading = false),
    });
  }

  loadEligible(): void {
    this.workspace.getEligibleUsers(this.eligibleSearch).subscribe({
      next: (r) => (this.eligiblePick = r),
      error: () => {},
    });
  }

  onEligibleSearchChange(): void {
    this.loadEligible();
  }

  addEmployee(): void {
    if (!this.addUserId) return;
    this.addingEmployee = true;
    this.error = null;
    this.workspace
      .addEmployee({
        userId: this.addUserId,
        jobTitle: this.addJobTitle || null,
        department: this.addDepartment || null,
      })
      .pipe(finalize(() => (this.addingEmployee = false)))
      .subscribe({
        next: () => {
          this.addUserId = '';
          this.addJobTitle = '';
          this.addDepartment = '';
          this.refreshEmployeesOnly();
        },
        error: (e) => {
          const msg = e?.error?.message || 'Could not add employee';
          this.error = msg;
        },
      });
  }

  removeEmployee(userId: string): void {
    if (!confirm('Remove this person from the company employee roster?')) return;
    this.workspace.removeEmployee(userId).subscribe({
      next: () => this.refreshEmployeesOnly(),
      error: () => (this.error = 'Could not remove employee'),
    });
  }

  refreshEmployeesOnly(): void {
    this.workspace.getEmployees().subscribe({ next: (r) => (this.employees = r) });
  }

  createTeam(): void {
    const name = this.newTeamName.trim();
    if (!name) return;
    this.creatingTeam = true;
    this.error = null;
    this.workspace
      .createTeam({ name, description: this.newTeamDescription.trim() || null })
      .pipe(finalize(() => (this.creatingTeam = false)))
      .subscribe({
        next: () => {
          this.newTeamName = '';
          this.newTeamDescription = '';
          this.workspace.getTeams().subscribe({ next: (r) => (this.teams = r) });
        },
        error: (e) => {
          this.error = e?.error?.message || 'Could not create team';
        },
      });
  }

  deleteTeam(team: WorkspaceTeam): void {
    if (!confirm(`Delete team "${team.name}" and its boards? This cannot be undone.`)) return;
    this.workspace.deleteTeam(team.id).subscribe({
      next: () => this.workspace.getTeams().subscribe({ next: (r) => (this.teams = r) }),
      error: () => (this.error = 'Could not delete team'),
    });
  }
}
