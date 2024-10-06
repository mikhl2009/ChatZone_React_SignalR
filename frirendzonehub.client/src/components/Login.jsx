import { Box, TextField, Button, Typography } from "@mui/material";
import axios from "axios";
import { useNavigate } from "react-router-dom";
import { useState } from "react";

const Login = () => {
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const navigate = useNavigate();

  const handleLogin = async () => {
    try {
      const response = await axios.post(
        "https://localhost:7167/api/auth/login",
        { username, password },
        {
          header: {
            "content-type": "application/json",
          },
        }
      );
      const token = response.data.token;

      if (token) {
        localStorage.setItem("authToken", token); // Store token in localStorage
        console.log("Login successful, token stored");

        // Redirect to the chat page after successful login
        navigate("/chat");
      } else {
        console.error("No token received");
      }
    } catch (error) {
      console.error("Login failed: ", error);
      alert("Login failed: " + error.response?.data);
    }
  };

  return (
    <Box display="flex" flexDirection="column" alignItems="center" mt={10}>
      <Typography variant="h4" mb={3}>
        Login
      </Typography>
      <TextField
        label="Username"
        value={username}
        onChange={(e) => setUsername(e.target.value)}
        fullWidth
        margin="normal"
      />
      <TextField
        label="Password"
        type="password"
        value={password}
        onChange={(e) => setPassword(e.target.value)}
        fullWidth
        margin="normal"
      />
      <Button
        variant="contained"
        color="primary"
        onClick={handleLogin}
        sx={{ mt: 2 }}
      >
        Login
      </Button>
    </Box>
  );
};

export default Login;
