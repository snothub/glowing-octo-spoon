import React, { useState, useEffect } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import { config } from '../config';
import { LoginFormData, LoginError } from '../types/auth';

const Login: React.FC = () => {
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  
  const location = useLocation();
  const navigate = useNavigate();
  const searchParams = new URLSearchParams(location.search);
  const returnUrl = searchParams.get('ReturnUrl');

  if (!returnUrl) {
    console.error(`ReturnUrl parameter is missing in the URL '${location.search}'`);
    return <div>Error: returnUrl parameter is required</div>;
  }

  useEffect(() => {
    const checkForExternalProvider = async () => {
      try {
        const response = await fetch(`${config.API_URL}/login/context?returnUrl=${encodeURIComponent(returnUrl)}`, {
          method: 'GET',
          credentials: 'include',
        });

        if (response.ok) {
          const data = await response.json();
          if (data.requiresExternalLogin) {
            // Redirect to external provider challenge
            window.location.href = `${config.API_URL}/ExternalLogin/Challenge?scheme=${data.externalProvider}&returnUrl=${encodeURIComponent(returnUrl)}`;
            return;
          }
        }
      } catch (err) {
        console.error('Error checking login context:', err);
      } finally {
        setIsLoading(false);
      }
    };

    checkForExternalProvider();
  }, [returnUrl]);

  if (isLoading) {
    return <div>Loading...</div>;
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);

    const formData: LoginFormData = {
      username,
      password,
      returnUrl
    };

    try {
      const response = await fetch(`${config.API_URL}/login`, {
        method: 'POST',
        credentials: 'include',
        headers: {
          'Content-Type': 'application/json'
        },
        body: JSON.stringify(formData)
      });

      if (response.ok) {
        const data = await response.json();
        window.location.href = data.returnUrl;
      } else {
        const errorData: LoginError = await response.json();
        setError(errorData.error);
      }
    } catch (err) {
      setError('An unexpected error occurred. Please try again.');
    }
  };

  const handleBack = () => {
    navigate(-1);
  };

  return (
    <div className="login-container">
      <form onSubmit={handleSubmit}>
        <input
          type="hidden"
          name="returnUrl"
          value={returnUrl}
        />
        
        <div className="form-group">
          <label htmlFor="username">Username:</label>
          <input
            id="username"
            type="text"
            value={username}
            onChange={(e) => setUsername(e.target.value)}
            required
          />
        </div>

        <div className="form-group">
          <label htmlFor="password">Password:</label>
          <input
            id="password"
            type="password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            required
          />
        </div>

        {error && (
          <div className="error-message">
            {error}
          </div>
        )}

        <div className="button-group">
          <button type="button" onClick={handleBack}>
            Back
          </button>
          <button type="submit">
            Submit
          </button>
        </div>
      </form>
    </div>
  );
};

export default Login;