import React, { useState } from "react";
import { Modal, Box, Typography, TextField, Button } from "@mui/material";

const AddMemberModal = ({ isOpen, onClose, onAddMember, room }) => {
  const [username, setUsername] = useState("");

  const handleAdd = () => {
    if (username.trim() === "") return;
    onAddMember(username);
    setUsername("");
  };

  return (
    <Modal open={isOpen} onClose={onClose}>
      <Box
        sx={{
          position: "absolute",
          top: "50%",
          left: "50%",
          transform: "translate(-50%, -50%)",
          bgcolor: "background.paper",
          p: 4,
          borderRadius: 1,
          boxShadow: 24,
          width: 300,
        }}
      >
        <Typography variant="h6" mb={2}>
          Add Member to {room?.name}
        </Typography>
        <TextField
          fullWidth
          label="Username"
          value={username}
          onChange={(e) => setUsername(e.target.value)}
          margin="normal"
        />
        <Box mt={2} display="flex" justifyContent="flex-end">
          <Button onClick={onClose} sx={{ mr: 1 }}>
            Cancel
          </Button>
          <Button variant="contained" onClick={handleAdd}>
            Add
          </Button>
        </Box>
      </Box>
    </Modal>
  );
};

export default AddMemberModal;
