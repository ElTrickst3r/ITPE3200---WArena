import * as signalR from '@microsoft/signalr'
import { api, getToken } from './api'

// REST calls for Live Arena rooms.
export const liveService = {
  createRoom: data => api.post('/api/live/rooms', data),
  joinRoom: code => api.post(`/api/live/rooms/${code}/join`, {}),
  start: code => api.post(`/api/live/rooms/${code}/start`, {}),
  next: code => api.post(`/api/live/rooms/${code}/next`, {}),
  answer: (code, answer, elapsedMs) => api.post(`/api/live/rooms/${code}/answer`, { answer, elapsedMs })
}

// Opens a SignalR connection to a room and calls onStateChanged whenever the server broadcasts a new state.
// Returns the connection so the caller can stop it.
export function connectToRoom(code, onStateChanged, onError) {
  const connection = new signalR.HubConnectionBuilder()
    .withUrl('/hubs/live', { accessTokenFactory: () => getToken() || '' })
    .withAutomaticReconnect()
    .build()

  connection.on('StateChanged', onStateChanged)
  // Group membership is lost when the connection drops, so the room has to be rejoined after reconnecting.
  connection.onreconnected(() => connection.invoke('JoinRoom', code).catch(err => onError(err.message)))
  connection.start()
    .then(() => connection.invoke('JoinRoom', code))
    .catch(err => onError(err.message))

  return connection
}