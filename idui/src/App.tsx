import React from 'react';
import { BrowserRouter as Router, Routes, Route } from 'react-router-dom';
import Login from './components/Login';
import Consent from './components/Consent';
import './styles/Login.css';
import './styles/Consent.css';

const App: React.FC = () => {
  return (
    <Router>
      <Routes>
        <Route path="/login" element={<Login />} />
        <Route path="/consent" element={<Consent />} />
      </Routes>
    </Router>
  );
};

export default App;