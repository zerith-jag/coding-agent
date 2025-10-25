import { Injectable, signal } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class SignalRService {
  private hubConnection: signalR.HubConnection | null = null;
  
  // Connection state signals
  public connectionState = signal<signalR.HubConnectionState>(
    signalR.HubConnectionState.Disconnected
  );
  public isConnected = signal<boolean>(false);

  constructor() {
    this.initializeConnection();
  }

  /**
   * Initialize SignalR hub connection
   */
  private initializeConnection(): void {
    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(environment.signalRUrl, {
        skipNegotiation: true,
        transport: signalR.HttpTransportType.WebSockets
      })
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Information)
      .build();

    // Connection state handlers
    this.hubConnection.onreconnecting(() => {
      this.connectionState.set(signalR.HubConnectionState.Reconnecting);
      this.isConnected.set(false);
    });

    this.hubConnection.onreconnected(() => {
      this.connectionState.set(signalR.HubConnectionState.Connected);
      this.isConnected.set(true);
    });

    this.hubConnection.onclose(() => {
      this.connectionState.set(signalR.HubConnectionState.Disconnected);
      this.isConnected.set(false);
    });
  }

  /**
   * Start the SignalR connection
   */
  public async connect(): Promise<void> {
    if (!this.hubConnection) {
      this.initializeConnection();
    }

    if (this.hubConnection!.state === signalR.HubConnectionState.Disconnected) {
      try {
        await this.hubConnection!.start();
        this.connectionState.set(signalR.HubConnectionState.Connected);
        this.isConnected.set(true);
        console.log('SignalR connected successfully');
      } catch (error) {
        console.error('Error connecting to SignalR hub:', error);
        throw error;
      }
    }
  }

  /**
   * Stop the SignalR connection
   */
  public async disconnect(): Promise<void> {
    if (this.hubConnection && this.hubConnection.state === signalR.HubConnectionState.Connected) {
      try {
        await this.hubConnection.stop();
        this.connectionState.set(signalR.HubConnectionState.Disconnected);
        this.isConnected.set(false);
        console.log('SignalR disconnected');
      } catch (error) {
        console.error('Error disconnecting from SignalR hub:', error);
        throw error;
      }
    }
  }

  /**
   * Register a handler for a specific hub method
   * @param methodName The name of the hub method to listen for
   * @param handler The callback function to execute when the method is invoked
   */
  public on<T>(methodName: string, handler: (data: T) => void): void {
    if (this.hubConnection) {
      this.hubConnection.on(methodName, handler);
    }
  }

  /**
   * Invoke a hub method
   * @param methodName The name of the hub method to invoke
   * @param args The arguments to pass to the hub method
   */
  public async invoke(methodName: string, ...args: any[]): Promise<any> {
    if (this.hubConnection && this.hubConnection.state === signalR.HubConnectionState.Connected) {
      try {
        return await this.hubConnection.invoke(methodName, ...args);
      } catch (error) {
        console.error(`Error invoking hub method ${methodName}:`, error);
        throw error;
      }
    } else {
      throw new Error('SignalR connection is not established');
    }
  }

  /**
   * Remove a handler for a specific hub method
   * @param methodName The name of the hub method to stop listening for
   */
  public off(methodName: string): void {
    if (this.hubConnection) {
      this.hubConnection.off(methodName);
    }
  }
}
