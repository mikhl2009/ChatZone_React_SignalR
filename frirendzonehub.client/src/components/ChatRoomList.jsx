import React, { useEffect, useState } from "react";
import {
  List,
  ListItemText,
  ListItemButton,
  CircularProgress,
  Box,
} from "@mui/material";
import axios from "axios";

const ChatRoomList = ({ selectedRoom, setSelectedRoom }) => {
  const [chatRooms, setChatRooms] = useState([]);
  const [loading, setLoading] = useState(true); // Loading state
  const [error, setError] = useState(null); // Error state

  useEffect(() => {
    const fetchChatRooms = async () => {
      try {
        const token = localStorage.getItem("token");
        const response = await axios.get(
          "https://localhost:7167/api/chatrooms",
          {
            headers: { Authorization: `Bearer ${token}` },
          }
        );
        setChatRooms(response.data);
      } catch (err) {
        console.error("Failed to fetch chat rooms:", err);
        setError("Failed to load chat rooms. Please try again.");
      } finally {
        setLoading(false);
      }
    };

    fetchChatRooms();
  }, []);

  if (loading) {
    return (
      <Box
        display="flex"
        justifyContent="center"
        alignItems="center"
        height="100%"
      >
        <CircularProgress />
      </Box>
    );
  }

  if (error) {
    return (
      <Box textAlign="center" mt={2}>
        {error}
      </Box>
    );
  }

  return (
    <List>
      {chatRooms.map((room) => (
        <ListItemButton
          key={room.id}
          selected={selectedRoom === room.name}
          onClick={() => setSelectedRoom(room.name)}
        >
          <ListItemText primary={room.name} />
        </ListItemButton>
      ))}
    </List>
  );
};

export default ChatRoomList;
