import React from "react";
import {
  Box,
  Button,
  Typography,
  Grid,
  Card,
  CardContent,
  Container,
  ThemeProvider,
  createTheme,
  useMediaQuery,
  useTheme,
} from "@mui/material";
import { MessageCircle, Users, Zap } from "lucide-react"; // Ensure you have lucide-react installed
import RollingImages from "./RollingImages"; // Ensure this is in the same folder

const theme = createTheme({
  palette: {
    primary: {
      main: "#3f51b5",
    },
    secondary: {
      main: "#f50057",
    },
  },
});

const FeatureCard = ({ icon, title, description }) => (
  <Card
    sx={{
      background: "rgba(255, 255, 255, 0.2)",
      textAlign: "center",
      borderRadius: 2,
      height: "200px", // Adjusted height for the feature cards
      display: "flex",
      flexDirection: "column",
      justifyContent: "center", // Center contents vertically
      padding: 3, // Padding for card content
    }}
  >
    <Box sx={{ fontSize: "2.5rem", mb: 1 }}>{icon}</Box>
    <Typography variant="h6" gutterBottom>
      {title}
    </Typography>
    <Typography variant="body2">{description}</Typography>
  </Card>
);

const WelcomePage = () => {
  const theme = useTheme();
  const isSmallScreen = useMediaQuery(theme.breakpoints.down("sm"));

  return (
    <ThemeProvider theme={theme}>
      <Box
        sx={{
          minHeight: "100vh",
          display: "flex",
          flexDirection: "column",
          alignItems: "center",
          justifyContent: "center",
          background: "linear-gradient(135deg, #3a6bfc 30%, #8d56f5 100%)", // Full page gradient
          color: "white",
          p: 2,
        }}
      >
        <Box
          sx={{
            width: "90%", // Set width to 90% for responsiveness
            maxWidth: 900, // Set a wider max width for the entire welcome box
            height: isSmallScreen ? "auto" : "75vh", // Adjust height based on screen size
            background: "rgba(255, 255, 255, 0.1)",
            backdropFilter: "blur(10px)",
            borderRadius: 2,
            p: 4,
            textAlign: "center",
            boxShadow: 3,
            display: "flex",
            flexDirection: "column",
            justifyContent: "space-between", // Distribute space
          }}
        >
          <div>
            <Typography variant={isSmallScreen ? "h5" : "h4"} gutterBottom>
              Welcome to FriendZone Chat
            </Typography>
            <Typography variant={isSmallScreen ? "body1" : "h6"} gutterBottom>
              Connect with friends and enjoy chatting!
            </Typography>
          </div>

          <Grid container spacing={2} justifyContent="center" mb={2}>
            <Grid item xs={12} sm={4}>
              <FeatureCard
                icon={<MessageCircle />}
                title="Real-time Chat"
                description="Instant messaging with friends"
              />
            </Grid>
            <Grid item xs={12} sm={4}>
              <FeatureCard
                icon={<Users />}
                title="Group Chats"
                description="Create and join group conversations"
              />
            </Grid>
            <Grid item xs={12} sm={4}>
              <FeatureCard
                icon={<Zap />}
                title="Fast & Secure"
                description="End-to-end encryption for your privacy"
              />
            </Grid>
          </Grid>

          <Box
            display="flex"
            flexDirection={isSmallScreen ? "column" : "row"}
            justifyContent="center"
            gap={2}
            mb={2}
          >
            <Button
              variant="contained"
              color="primary"
              onClick={() => (window.location.href = "/login")}
              sx={{
                borderRadius: 20,
                paddingX: 3,
                paddingY: 1,
                width: isSmallScreen ? "100%" : "auto",
              }} // Rounded and larger buttons, full width on small screens
            >
              Log In
            </Button>
            <Button
              variant="contained"
              color="secondary"
              onClick={() => (window.location.href = "/signup")}
              sx={{
                borderRadius: 20,
                paddingX: 3,
                paddingY: 1,
                width: isSmallScreen ? "100%" : "auto",
              }} // Rounded and larger buttons, full width on small screens
            >
              Sign Up
            </Button>
          </Box>

          <RollingImages />
        </Box>
      </Box>
    </ThemeProvider>
  );
};

export default WelcomePage;
