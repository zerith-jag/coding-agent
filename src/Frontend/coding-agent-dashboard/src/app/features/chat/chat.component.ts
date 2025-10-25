import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';

@Component({
  selector: 'app-chat',
  standalone: true,
  imports: [CommonModule, MatCardModule],
  template: `
    <div class="chat-container">
      <mat-card>
        <mat-card-header>
          <mat-card-title>Chat</mat-card-title>
        </mat-card-header>
        <mat-card-content>
          <p>Real-time chat interface will be implemented here.</p>
          <p>SignalR service is ready for integration.</p>
        </mat-card-content>
      </mat-card>
    </div>
  `,
  styles: [`
    .chat-container {
      max-width: 1200px;
      margin: 0 auto;
      padding: 24px;
    }
  `]
})
export class ChatComponent {}
