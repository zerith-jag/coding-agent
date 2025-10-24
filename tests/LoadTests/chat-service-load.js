import http from 'k6/http';
import ws from 'k6/ws';
import { check, sleep } from 'k6';
import { Rate, Trend, Counter } from 'k6/metrics';

// Custom metrics
const errorRate = new Rate('errors');
const messageLatency = new Trend('message_latency');
const conversationsCreated = new Counter('conversations_created');
const messagessent = new Counter('messages_sent');

// Test configuration
export const options = {
  stages: [
    { duration: '30s', target: 10 },   // Warm-up: Ramp up to 10 users
    { duration: '1m', target: 50 },    // Load: Ramp up to 50 users
    { duration: '2m', target: 50 },    // Sustain: Stay at 50 users
    { duration: '30s', target: 0 },    // Cool down: Ramp down to 0 users
  ],
  thresholds: {
    'http_req_duration': ['p(95)<100'],      // 95% of HTTP requests < 100ms
    'http_req_failed': ['rate<0.05'],        // HTTP errors < 5%
    'errors': ['rate<0.05'],                 // Overall error rate < 5%
    'message_latency': ['p(95)<100'],        // Message latency < 100ms
    'websocket_connecting{status:101}': ['rate>0.95'],  // WebSocket success > 95%
  },
  summaryTrendStats: ['min', 'avg', 'med', 'p(90)', 'p(95)', 'p(99)', 'max'],
};

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5000';
const WS_URL = __ENV.WS_URL || 'ws://localhost:5000';

/**
 * Main test scenario - simulates a user creating conversation and sending messages
 */
export default function () {
  // NOTE: This load test is ready to run once Gateway and Chat Service are implemented
  // Until then, it will fail with connection errors
  
  // Test 1: Create conversation via REST API
  const createPayload = JSON.stringify({
    title: `Load Test Conversation ${__VU}-${__ITER}`,
  });

  const createHeaders = {
    'Content-Type': 'application/json',
  };

  const createRes = http.post(
    `${BASE_URL}/api/conversations`,
    createPayload,
    { headers: createHeaders }
  );

  const conversationCreated = check(createRes, {
    'conversation created': (r) => r.status === 201,
    'response has conversation id': (r) => {
      try {
        const json = JSON.parse(r.body);
        return json.id !== undefined;
      } catch {
        return false;
      }
    },
  });

  if (!conversationCreated) {
    errorRate.add(1);
    console.error(`Failed to create conversation: ${createRes.status}`);
    sleep(1);
    return;
  }

  conversationsCreated.add(1);
  const conversation = JSON.parse(createRes.body);

  // Test 2: Connect via WebSocket and send messages
  const wsUrl = `${WS_URL}/hubs/chat`;
  
  const wsRes = ws.connect(wsUrl, {}, function (socket) {
    socket.on('open', function () {
      // SignalR handshake
      const handshake = JSON.stringify({
        protocol: 'json',
        version: 1,
      });
      socket.send(handshake + '\x1e');

      // Join conversation
      const joinMessage = JSON.stringify({
        type: 1,
        target: 'JoinConversation',
        arguments: [conversation.id],
      });
      socket.send(joinMessage + '\x1e');

      // Send message and measure latency
      const startTime = new Date();
      const sendMessage = JSON.stringify({
        type: 1,
        target: 'SendMessage',
        arguments: [conversation.id, `Load test message ${__VU}-${__ITER}`],
      });
      socket.send(sendMessage + '\x1e');

      messagesent.add(1);

      // Handle message response
      socket.on('message', function (data) {
        const latency = new Date() - startTime;
        messageLatency.add(latency);

        check(data, {
          'message received': (d) => d !== null,
        });
      });
    });

    socket.on('error', function (e) {
      console.error(`WebSocket error: ${e}`);
      errorRate.add(1);
    });

    socket.on('close', function () {
      // Connection closed normally
    });

    // Keep connection open for 5 seconds
    socket.setTimeout(function () {
      socket.close();
    }, 5000);
  });

  check(wsRes, {
    'websocket connected': (r) => r && r.status === 101,
  });

  // Test 3: Fetch conversation via REST API (test caching)
  const getRes = http.get(`${BASE_URL}/api/conversations/${conversation.id}`);
  
  check(getRes, {
    'get conversation successful': (r) => r.status === 200,
    'conversation data matches': (r) => {
      try {
        const json = JSON.parse(r.body);
        return json.id === conversation.id;
      } catch {
        return false;
      }
    },
  });

  // Test 4: Fetch messages (test pagination)
  const messagesRes = http.get(`${BASE_URL}/api/conversations/${conversation.id}/messages`);
  
  check(messagesRes, {
    'get messages successful': (r) => r.status === 200,
    'messages array returned': (r) => {
      try {
        const json = JSON.parse(r.body);
        return Array.isArray(json);
      } catch {
        return false;
      }
    },
  });

  // Simulate user think time
  sleep(1);
}

/**
 * Setup function - runs once before test
 */
export function setup() {
  console.log('=================================================');
  console.log('Starting POC Load Test');
  console.log(`Base URL: ${BASE_URL}`);
  console.log(`WebSocket URL: ${WS_URL}`);
  console.log('=================================================');
  
  // Check if services are available
  const healthRes = http.get(`${BASE_URL}/health`, { timeout: '5s' });
  if (healthRes.status !== 200) {
    console.warn('WARNING: Services may not be running!');
    console.warn('Start services with: docker compose -f deployment/docker-compose/docker-compose.dev.yml up -d');
  }
  
  return { timestamp: new Date().toISOString() };
}

/**
 * Teardown function - runs once after test
 */
export function teardown(data) {
  console.log('=================================================');
  console.log('Load Test Complete');
  console.log(`Started at: ${data.timestamp}`);
  console.log(`Ended at: ${new Date().toISOString()}`);
  console.log('=================================================');
}
