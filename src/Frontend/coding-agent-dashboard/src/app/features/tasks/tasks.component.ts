import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';

@Component({
  selector: 'app-tasks',
  standalone: true,
  imports: [CommonModule, MatCardModule],
  template: `
    <div class="tasks-container">
      <mat-card>
        <mat-card-header>
          <mat-card-title>Tasks</mat-card-title>
        </mat-card-header>
        <mat-card-content>
          <p>Task management interface will be implemented here.</p>
          <p>View, create, and manage coding tasks.</p>
        </mat-card-content>
      </mat-card>
    </div>
  `,
  styles: [`
    .tasks-container {
      max-width: 1200px;
      margin: 0 auto;
      padding: 24px;
    }
  `]
})
export class TasksComponent {}
