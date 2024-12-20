### **U_Ride: University Ride Sharing App**  

U_Ride is a ride-sharing application designed to connect university students with drivers within the campus environment. It provides a seamless platform for students to request rides and communicate with available drivers. The project focuses on efficient backend implementation with real-time communication, secure data handling, and robust relational database management.  

---

### **Features**
- **Ride Management**: Students can request rides, and drivers can post available rides with specified start and stop points.  
- **Chat Functionality**: Students can initiate real-time chats with drivers using SignalR. The chat messages are stored securely for future reference.  
- **Distance Calculation**: Calculates the distance between ride start and stop points using precise geographic calculations.  
- **Real-Time Communication**: Implements WebSocket-based communication via SignalR to ensure smooth and instant messaging between users.  

---

### **Technologies Used**
- **Backend**: ASP.NET Core 8  
- **Database**: Microsoft SQL Server (Entity Framework Core for ORM)  
- **Real-Time Communication**: SignalR  
- **Authentication**: JWT (JSON Web Tokens)  
- **Programming Language**: C#  

---

### **Database Schema**
#### **Main Tables**
- **Users**: Stores user information (students and drivers).
- **Rides**: Stores ride information (students and drivers).
- **Vehicles**: Stores driver vehicle information.
- **Chats**: Tracks chat sessions between students and drivers, including start timestamps.  
- **Messages**: Saves all messages within a chat session, linked to the `Chats` table.  

---

### **How It Works**
1. **Ride Posting**:
   - Drivers post available rides by specifying start and stop locations.  
   - The system calculates the route and divides it into key points.  

2. **Chat Initiation**:
   - A student and driver both can initiates a chat with each other.  
   - SignalR manages real-time communication, ensuring a smooth user experience.  

3. **Message Storage**:
   - Messages exchanged in chats are stored in the `Messages` table with timestamps for future reference.  

4. **Driver Interaction**:
   - Drivers are notified of new messages and ride confirmation/cancellation and can respond directly through the chat system.

---

### **Architecture Flowchart**
For a detailed understanding of the project’s architecture, including workflows for ride calculations and real-time chat, refer to the flowchart below:

#### **Flowchart**
![Processflow](https://github.com/user-attachments/assets/f215f058-19ae-4be4-9747-830c2b9ffb89)

---

### **How to Test Without Frontend**
1. **SignalR Hub Connection**:
   - Use tools like [Postman](https://www.postman.com/) or [Swagger](https://swagger.io/) to test the APIs.  
   - Connect to the SignalR hub using a WebSocket client like [wscat](https://github.com/websockets/wscat).  

2. **Testing Steps**:
   - Initiate a chat by posting a message to the chat API with `ChatID` and `SenderID`.  
   - Use SignalR to receive and send messages in real time.  

3. **Database Validation**:
   - Verify the `Chats` and `Messages` tables in the database to ensure data integrity.

---

### **Setup Instructions**
1. Clone the repository:
   ```bash
   git clone https://github.com/your-username/U_Ride.git
   cd U_Ride
   ```
2. Configure the database connection in `appsettings.json`.
3. Apply migrations and update the database:
   ```bash
   dotnet ef migrations add InitialCreate
   dotnet ef database update
   ```
4. Run the application:
   ```bash
   dotnet run
   ```

---

### **Contributing**
Contributions are welcome! Please submit a pull request or open an issue to suggest improvements.  

---

### **License**
This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.  
