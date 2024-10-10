import React, { useState } from "react";
import {
  Modal,
  Box,
  Typography,
  TextField,
  Checkbox,
  FormControlLabel,
  Button,
} from "@mui/material";

const CreateRoomModal = ({ isOpen, onClose, onCreate }) => {
  const [roomName, setRoomName] = useState("");
  const [isPrivate, setIsPrivate] = useState(false);

  const handleCreate = () => {
    if (roomName.trim() === "") return;
    onCreate({ roomName, isPrivate });
    setRoomName("");
    setIsPrivate(false);
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
          Create New Room
        </Typography>
        <TextField
          fullWidth
          label="Room Name"
          value={roomName}
          onChange={(e) => setRoomName(e.target.value)}
          margin="normal"
        />
        <FormControlLabel
          control={
            <Checkbox
              checked={isPrivate}
              onChange={(e) => setIsPrivate(e.target.checked)}
            />
          }
          label="Private Room"
        />
        <Box mt={2} display="flex" justifyContent="flex-end">
          <Button onClick={onClose} sx={{ mr: 1 }}>
            Cancel
          </Button>
          <Button variant="contained" onClick={handleCreate}>
            Create
          </Button>
        </Box>
      </Box>
    </Modal>
  );
};

export default CreateRoomModal;
