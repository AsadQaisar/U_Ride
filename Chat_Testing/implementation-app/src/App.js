import React, { useState, useEffect } from "react";
import axios from "axios";
import * as signalR from "@microsoft/signalr";
import "bootstrap/dist/css/bootstrap.min.css";

const App = () => {
  const [role, setRole] = useState("Driver"); // Default role
  const [token, setToken] = useState(""); // Token entered by user
  const [clientID, setClientID] = useState(""); // Dynamic client ID from SignalR
  const [userID, setUserID] = useState("");
  const [privateMessages, setPrivateMessages] = useState([]); // Store private messages
  const [formData, setFormData] = useState({
    startPoint: "",
    endPoint: "",
    distance: "",
    price: "", // Only for PostRide
    encodedPolyline: "",
  });

  // SignalR Connection Setup
  useEffect(() => {
    let connection = null;
  
    const initializeConnection = (userToken) => {
      connection = new signalR.HubConnectionBuilder()
        .withUrl("https://localhost:7213/Chat", {
          accessTokenFactory: () => userToken, // Pass token for authentication
        })
        .withAutomaticReconnect()
        .build();
  
      connection
        .start()
        .then(() => {
          console.log("SignalR connected.");
        })
        .catch((error) => console.error("SignalR connection failed:", error));
  
      connection.on("privateMessageMethodName", (data) => {
        setPrivateMessages((prevMessages) => [...prevMessages, data]);
      });
  
      connection.on("TokenReceived", (data) => {
        console.log("Token Status:", data);
      });

      connection.on("WelcomeMethodName", (connectionId, userId) => {
        setClientID(connectionId); // Use connectionId for client-specific tasks
        setUserID(userId);
        console.log("Client ID:", connectionId);
        console.log("User ID:", userId); 
      });
  
    };
  
    if (token) {
      initializeConnection(token);
    }
  
    return () => {
      if (connection) {
        connection.stop().then(() => console.log("SignalR disconnected."));
      }
    };
  }, [token]);  

  // Trigger connection when token is entered
  const handleTokenChange = (e) => {
    const newToken = e.target.value;
    setToken(newToken);
  };

  const handleInputChange = (e) => {
    const { name, value } = e.target;
    setFormData((prevData) => ({
      ...prevData,
      [name]: value,
    }));
  };

  const handleSubmit = async (e) => {
    e.preventDefault();

    const dataToSend = {
      startPoint: formData.startPoint,
      endPoint: formData.endPoint,
      distance: parseFloat(formData.distance),
      encodedPolyline: formData.encodedPolyline,
      ...(role === "Driver" && { price: parseFloat(formData.price) }), // Add price only for drivers
    };

    try {
      const endpoint =
        role === "Driver"
          ? "https://localhost:7213/Driver/PostRide"
          : "https://localhost:7213/Student/SearchRides";

      const response = await axios.post(endpoint, dataToSend, {
        headers: {
          Authorization: `Bearer ${token}`,
        },
      });

      console.log("API Response:", response.data);
      alert("Data sent successfully!");
    } catch (error) {
      console.error("API Error:", error.response?.data || error.message);
      alert("Failed to send data. Check the console for details.");
    }
  };

  const sendMessage = async (e) => {
    e.preventDefault();
    try {
      console.log(token);
      const response = await axios.post(
        "https://localhost:7213/Chat/SendPrivateMessage",
        {
          // connectionID: e.target.clientId.value,
          receiverID: parseInt(e.target.receiverID.value),
          message: e.target.message.value,
        },
        {
          headers: {
            Authorization: `Bearer ${token}`, 
          },
        }
      );
      console.log("Message sent:", response.data);
    } catch (error) {
      console.error("Error sending message:", error.response?.data || error.message);
      alert("Failed to send the message.");
    }
  };

  return (
    <div className="container py-4">
      <h1 className="mb-3 text-center">Ride Sharing Application</h1>

      <div className="row">
        {/* Ride Form */}
        <div className="col-md-8">
          <div className="d-flex justify-content-evenly">
            {/* Role Selection */}
            <div className="mb-2">
              <label htmlFor="role" className="form-label">
                Select Role:
              </label>
              <select
                id="role"
                className="form-select"
                value={role}
                onChange={(e) => setRole(e.target.value)}
              >
                <option value="Driver">Driver</option>
                <option value="Student">Student</option>
              </select>
            </div>

            {/* Token Input */}
            <div className="mb-2">
              <label htmlFor="token" className="form-label">
                Enter Token:
              </label>
              <input
                id="token"
                type="text"
                className="form-control"
                value={token}
                onChange={handleTokenChange}
                placeholder="Enter your API token"
                required
              />
            </div>
          </div>
          <form onSubmit={handleSubmit}>
            <div className="mb-2">
              <label htmlFor="startPoint" className="form-label">
                Start Point:
              </label>
              <input
                id="startPoint"
                type="text"
                name="startPoint"
                className="form-control"
                value={formData.startPoint}
                onChange={handleInputChange}
                required
              />
            </div>
            <div className="mb-2">
              <label htmlFor="endPoint" className="form-label">
                End Point:
              </label>
              <input
                id="endPoint"
                type="text"
                name="endPoint"
                className="form-control"
                value={formData.endPoint}
                onChange={handleInputChange}
                required
              />
            </div>
            <div className="mb-2">
              <label htmlFor="distance" className="form-label">
                Distance (meters):
              </label>
              <input
                id="distance"
                type="number"
                name="distance"
                className="form-control"
                value={formData.distance}
                onChange={handleInputChange}
                required
              />
            </div>
            {role === "Driver" && (
              <div className="mb-2">
                <label htmlFor="price" className="form-label">
                  Price:
                </label>
                <input
                  id="price"
                  type="number"
                  name="price"
                  className="form-control"
                  value={formData.price}
                  onChange={handleInputChange}
                  required
                />
              </div>
            )}
            <div className="mb-3">
              <label htmlFor="encodedPolyline" className="form-label">
                Encoded Polyline:
              </label>
              <textarea
                id="encodedPolyline"
                name="encodedPolyline"
                className="form-control"
                rows="2"
                value={formData.encodedPolyline}
                onChange={handleInputChange}
                required
              ></textarea>
            </div>
            <button type="submit" className="btn btn-dark w-100">
              {role === "Driver" ? "Post Ride" : "Search Ride"}
            </button>
          </form>
        </div>

        {/* Private Messages Box */}
        <div className="col-md-4">
          <div className="border rounded my-2 p-3 bg-light">
            <h5>Private Messages</h5>
            <p className="fw-bold mb-1">Client ID: {clientID || "Not available"}</p>
            <p className="fw-bold">User ID: {userID || "Not available"}</p>
            <ul
              className="list-group"
              style={{
                maxHeight: "100px", // Limit height for scrollable area
                overflowY: "auto",  // Enable vertical scrolling
              }}
            >
              {privateMessages.length > 0 ? (
                privateMessages.map((msg, index) => (
                  <li key={index} className="list-group-item">
                    {msg}
                  </li>
                ))
              ) : (
                <li className="list-group-item">No messages yet.</li>
              )}
            </ul>
          </div>

          {/* Send Message Box */}
          <div className="border rounded my-4 p-3 bg-light">
            <h5>Send Private Message</h5>
            <form onSubmit={sendMessage}>
              <div className="mb-2">
                <label htmlFor="receiverID" className="form-label">
                  Receiver ID:
                </label>
                <input
                  id="receiverID"
                  name="receiverID"
                  type="number"
                  className="form-control"
                  placeholder="Enter Receiver ID"
                  required
                />
              </div>
              {/* <div className="mb-2">
                <label htmlFor="connectionID" className="form-label">
                  Client ID:
                </label>
                <input
                  id="clientId"
                  name="clientId"
                  type="text"
                  className="form-control"
                  placeholder="Enter Client ID"
                  required
                />
              </div> */}
              <div className="mb-2">
                <label htmlFor="message" className="form-label">
                  Message:
                </label>
                <textarea
                  id="message"
                  name="message"
                  className="form-control"
                  placeholder="Enter your message"
                  rows="1"
                  required
                ></textarea>
              </div>
              <button type="submit" className="btn btn-dark w-100">
                Send Message
              </button>
            </form>
          </div>
        </div>
      </div>
    </div>
  );
};

export default App;
