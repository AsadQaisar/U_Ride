import React, { useState, useEffect } from "react";
import * as signalR from "@microsoft/signalr";
import "bootstrap/dist/css/bootstrap.min.css";

const App = () => {
  const [token, setToken] = useState(""); // Store token
  const [receiverId, setReceiverId] = useState(""); // Store receiverId
  const [message, setMessage] = useState(""); // Store message input
  const [messages, setMessages] = useState([]); // Store messages
  const [connection, setConnection] = useState(null); // SignalR connection
  const [clientId, setClientId] = useState("");
  const [userId, setUserId] = useState("");

  // Establish SignalR Connection
  useEffect(() => {
    if (!token) return;
  
    const connectToSignalR = async () => {
      const conn = new signalR.HubConnectionBuilder()
        .withUrl("https://localhost:7213/Chat", {
          accessTokenFactory: () => token,
        })
        .withAutomaticReconnect()
        .build();
  
      conn.on("WelcomeMessage", (connectionId, userId) => {
        setClientId(connectionId);
        setUserId(userId);
      });
  
      conn.on("IntroMessage", (data) => {
        const { ChatID, UserInfo } = data;
        console.log("Chat ID:", data.chatID);
        console.log("User Information: ", data.userInfo);
      });

      conn.on("ReceiveMessage", (senderId, receivedMessage) => {
        setMessages((prev) => [...prev, { senderId, message: receivedMessage }]);
      });
  
      try {
        await conn.start();
        console.log("SignalR connected.");
        setConnection(conn);
  
        conn.invoke("JoinGroup").catch((error) => {
          console.error("Error joining group:", error);
        });
      } catch (error) {
        console.error("SignalR connection error:", error);
      }
    };
  
    let connectionInstance = null;
  
    // Start connection
    connectToSignalR().then((conn) => {
      connectionInstance = conn;
    });
  
    // Cleanup on unmount or token change
    return () => {
      if (connectionInstance) {
        connectionInstance.stop().catch((error) => {
          console.error("Error stopping connection:", error);
        });
      }
    };
  }, [token]); // Only re-run when `token` changes
  

  // Send message to backend API
  const sendMessage = async (e) => {
    e.preventDefault();
    if (token && receiverId && message) {
      try {
        const response = await fetch("https://localhost:7213/Chat/SendPersonalMessage", {
          method: "POST",
          headers: {
            "Content-Type": "application/json",
            Authorization: `Bearer ${token}`,
          },
          body: JSON.stringify({
            ReceiverID: receiverId,
            Message: message,
          }),
        });

        if (response.ok) {
          // Send the message locally as well
          setMessages((prev) => [...prev, { senderId: "You", message }]);
          setMessage(""); // Clear input after sending
        } else {
          console.error("Failed to send message:", response.statusText);
        }
      } catch (error) {
        console.error("Error sending message:", error);
      }
    }
  };

  return (
    <div className="container py-4">
      <h2 className="text-center mb-5">SignalR Private Chat</h2>
      <div className="row">
        <div className="col-md-4 mb-4">
          <p>
            <strong>Client ID:</strong> {clientId || "Not available"}
          </p>
          <p>
            <strong>Your User ID:</strong> {userId || "Not available"}
          </p>
          <div className="mb-3">
            <label className="form-label">Token:</label>
            <input
              type="text"
              value={token}
              onChange={(e) => setToken(e.target.value)}
              className="form-control"
            />
          </div>
          <div className="mb-3">
            <label className="form-label">Receiver ID:</label>
            <input
              type="text"
              value={receiverId}
              onChange={(e) => setReceiverId(e.target.value)}
              className="form-control"
            />
          </div>
          <form onSubmit={sendMessage} className="mb-3">
            <label htmlFor="message" className="form-label">
              Message:
            </label>
            <textarea
              id="message"
              name="message"
              className="form-control"
              placeholder="Enter your message"
              rows="3"
              value={message}
              onChange={(e) => setMessage(e.target.value)}
              required
            ></textarea>
            <button type="submit" className="btn btn-dark mt-3 w-100">
              Send
            </button>
          </form>
        </div>
        <div className="col-md-8">
          <h4>Messages:</h4>
          <ul
            className="list-group"
            style={{ height: "250px", overflowY: "scroll" }}
          >
            {messages.map((msg, index) => (
              <li key={index} className="list-group-item">
                <strong>{msg.senderId}:</strong> {msg.message}
              </li>
            ))}
          </ul>
        </div>
      </div>
    </div>
  );
};

export default App;
