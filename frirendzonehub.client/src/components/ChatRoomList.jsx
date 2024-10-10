// ChatRoomList.jsx
import React, { useEffect, useState } from "react";
import {
  List,
  ListItemText,
  ListItemButton,
  CircularProgress,
  Box,
  Typography,
  Button,
  IconButton,
  Snackbar,
  Alert,
} from "@mui/material";
import AddIcon from "@mui/icons-material/Add";
import CreateRoomModal from "./CreateRoomModal";
import AddMemberModal from "./AddMemberModal";
import axios from "axios";
import { decodeJwt } from "jose"; // Only import decodeJwt

const ChatRoomList = ({ selectedRoom, setSelectedRoom }) => {
  const [chatRooms, setChatRooms] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [isCreateModalOpen, setCreateModalOpen] = useState(false);
  const [isAddMemberModalOpen, setAddMemberModalOpen] = useState(false);
  const [currentRoom, setCurrentRoom] = useState(null);
  const [currentUserId, setCurrentUserId] = useState(null);
  const [snackbar, setSnackbar] = useState({
    open: false,
    message: "",
    severity: "success", 
  });

  const handleCloseSnackbar = () => {
    setSnackbar({ ...snackbar, open: false });
  };

  // Function to decode JWT without verification
  const decodeToken = (token) => {
    try {
      const decoded = decodeJwt(token);
      console.log("Decoded JWT:", decoded);
      return decoded;
    } catch (error) {
      console.error("Failed to decode JWT:", error);
      return null;
    }
  };

  // Initialize current user ID from token
  useEffect(() => {
    const initializeUser = () => {
      const token = localStorage.getItem("authToken");
      if (token) {
        try {
          const decodedToken = decodeToken(token);
          if (decodedToken) {
            const userId = Number(decodedToken.uid); // Convert to number
            console.log("Extracted User ID (Number):", userId);
            setCurrentUserId(userId);
          }
        } catch (error) {
          console.error("Failed to decode token:", error);
        }
      }
    };
    initializeUser();
  }, []);

  // Fetch chat rooms from the server
  const fetchChatRooms = async () => {
    try {
      const token = localStorage.getItem("authToken");
      const response = await axios.get("https://localhost:7167/api/chatrooms", {
        headers: { Authorization: `Bearer ${token}` },
      });
      console.log("Fetched Chat Rooms:", response.data);
      setChatRooms(response.data);
    } catch (err) {
      console.error("Failed to fetch chat rooms:", err);
      setError("Failed to load chat rooms. Please try again.");
      setSnackbar({
        open: true,
        message: "Failed to load chat rooms.",
        severity: "error",
      });
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchChatRooms();
  }, []);

  // Handle room creation
  const handleCreateRoom = async ({ roomName, isPrivate }) => {
    try {
      const token = localStorage.getItem("authToken");
      const response = await axios.post(
        "https://localhost:7167/api/chatrooms",
        { name: roomName, isPrivate },
        {
          headers: { Authorization: `Bearer ${token}` },
        }
      );
      if (response.status === 201) {
        setCreateModalOpen(false);
        setSnackbar({
          open: true,
          message: "Room created successfully!",
          severity: "success",
        });
        fetchChatRooms(); // Refresh the list
      } else {
        console.error("Failed to create room.");
        setSnackbar({
          open: true,
          message: "Failed to create room.",
          severity: "error",
        });
      }
    } catch (err) {
      console.error("Failed to create room:", err);
      setSnackbar({
        open: true,
        message: "Failed to create room.",
        severity: "error",
      });
    }
  };

  // Open AddMemberModal
  const handleAddMemberClick = (room) => {
    setCurrentRoom(room);
    setAddMemberModalOpen(true);
  };

  // Handle adding a member
  const handleAddMember = async (username) => {
    try {
      const token = localStorage.getItem("authToken");
      const response = await axios.post(
        `https://localhost:7167/api/chatrooms/${currentRoom.id}/addmember`,
        { username },
        {
          headers: { Authorization: `Bearer ${token}` },
        }
      );
      if (response.status === 200) {
        setAddMemberModalOpen(false);
        setSnackbar({
          open: true,
          message: "Member added successfully!",
          severity: "success",
        });
        fetchChatRooms(); // Refresh chat rooms to show updated members
      } else {
        console.error("Failed to add member.");
        setSnackbar({
          open: true,
          message: "Failed to add member.",
          severity: "error",
        });
      }
    } catch (err) {
      console.error("Failed to add member:", err);
      setSnackbar({
        open: true,
        message: err.response?.data || "Failed to add member.",
        severity: "error",
      });
    }
  };

  // Conditional rendering for loading and error states
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
        <Typography color="error">{error}</Typography>
      </Box>
    );
  }

  return (
    <Box>
      {/* Button to open CreateRoomModal */}
      <Button
        variant="contained"
        color="primary"
        fullWidth
        onClick={() => setCreateModalOpen(true)}
        sx={{ mb: 2 }}
      >
        Create Room
      </Button>

      {/* CreateRoomModal Component */}
      <CreateRoomModal
        isOpen={isCreateModalOpen}
        onClose={() => setCreateModalOpen(false)}
        onCreate={handleCreateRoom}
      />

      {/* AddMemberModal Component */}
      <AddMemberModal
        isOpen={isAddMemberModalOpen}
        onClose={() => setAddMemberModalOpen(false)}
        onAddMember={handleAddMember}
        room={currentRoom}
      />

      {/* List of Chat Rooms */}
      <List>
        {chatRooms.map((room) => {
          console.log(
            `Room: ${room.name}, Admin ID: ${room.admin?.id}, Current User ID: ${currentUserId}`
          );
          return (
            <ListItemButton
              key={room.id}
              selected={selectedRoom === room.name}
              onClick={() => setSelectedRoom(room.name)}
              sx={{ display: "flex", justifyContent: "space-between" }} // Flex layout
            >
              <ListItemText
                primary={
                  <>
                    {room.name}{" "}
                    {room.isPrivate && (
                      <Typography variant="caption" color="textSecondary">
                        [Private]
                      </Typography>
                    )}
                  </>
                }
              />
              {/* Conditionally render the "+" icon for private rooms where the user is admin */}
              {room.isPrivate && room.admin?.id === currentUserId && (
                <IconButton
                  edge="end"
                  onClick={(e) => {
                    e.stopPropagation(); // Prevent triggering room selection
                    handleAddMemberClick(room);
                  }}
                  aria-label={`Add member to ${room.name}`}
                >
                  <AddIcon />
                </IconButton>
              )}
            </ListItemButton>
          );
        })}
      </List>

      {/* Snackbar for User Feedback */}
      <Snackbar
        open={snackbar.open}
        autoHideDuration={6000}
        onClose={handleCloseSnackbar}
      >
        <Alert
          onClose={handleCloseSnackbar}
          severity={snackbar.severity}
          sx={{ width: "100%" }}
        >
          {snackbar.message}
        </Alert>
      </Snackbar>
    </Box>
  );
};

export default ChatRoomList;
