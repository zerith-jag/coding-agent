import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, MatCardModule],
  template: `
    <div class="dashboard-container">
      <h1>Welcome to Coding Agent Dashboard</h1>
      <p>This is the main dashboard page. More features will be added soon.</p>
      
      <div class="dashboard-cards">
        <mat-card>
          <mat-card-header>
            <mat-card-title>Chat</mat-card-title>
          </mat-card-header>
          <mat-card-content>
            <p>Real-time chat with AI agents powered by SignalR.</p>
          </mat-card-content>
        </mat-card>

        <mat-card>
          <mat-card-header>
            <mat-card-title>Tasks</mat-card-title>
          </mat-card-header>
          <mat-card-content>
            <p>View and manage your coding tasks.</p>
          </mat-card-content>
        </mat-card>

        <mat-card>
          <mat-card-header>
            <mat-card-title>Statistics</mat-card-title>
          </mat-card-header>
          <mat-card-content>
            <p>Track your task completion and performance metrics.</p>
          </mat-card-content>
        </mat-card>
      </div>
    </div>
  `,
  styles: [`
    .dashboard-container {
      max-width: 1200px;
      margin: 0 auto;
      padding: 24px;
    }

    h1 {
      margin-bottom: 16px;
      color: #333;
    }

    p {
      color: #666;
      margin-bottom: 32px;
    }

    .dashboard-cards {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
      gap: 24px;
    }

    mat-card {
      mat-card-content {
        padding-top: 16px;
      }
    }
  `]
})
export class DashboardComponent {}
