import React, { useState, useEffect } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import { config } from '../config';
import { ConsentViewModel, ConsentInputModel } from '../types/consent';

const Consent: React.FC = () => {
  const [consentData, setConsentData] = useState<ConsentViewModel | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [selectedScopes, setSelectedScopes] = useState<Set<string>>(new Set());
  const [rememberChoice, setRememberChoice] = useState(false);
  
  const location = useLocation();
  const navigate = useNavigate();
  const searchParams = new URLSearchParams(location.search);
  const returnUrl = searchParams.get('returnUrl');

  useEffect(() => {
    const fetchConsentData = async () => {
      if (!returnUrl) {
        setError('returnUrl parameter is required');
        setLoading(false);
        return;
      }

      try {
        const response = await fetch(`${config.API_URL}/consent`, {
          method: 'POST',
          credentials: 'include',
          headers: {
            'Content-Type': 'application/json'
          },
          body: JSON.stringify({ returnUrl })
        });

        if (response.ok) {
          const data = await response.json();
          setConsentData(data);
        } else {
          const errorData = await response.json();
          setError(errorData.error || 'Failed to fetch consent data');
        }
      } catch (err) {
        setError('An unexpected error occurred while fetching consent data');
      } finally {
        setLoading(false);
      }
    };

    fetchConsentData();
  }, [returnUrl]);

  useEffect(() => {
    if (consentData) {
      const initialScopes = new Set([
        ...consentData.identityScopes
          .filter(scope => scope.checked || scope.required)
          .map(scope => scope.value),
        ...consentData.apiScopes
          .filter(scope => scope.checked || scope.required)
          .map(scope => scope.value)
      ]);
      setSelectedScopes(initialScopes);
    }
  }, [consentData]);

  const handleScopeChange = (scopeValue: string, checked: boolean) => {
    const newSelectedScopes = new Set(selectedScopes);
    if (checked) {
      newSelectedScopes.add(scopeValue);
    } else {
      newSelectedScopes.delete(scopeValue);
    }
    setSelectedScopes(newSelectedScopes);
  };

  const handleConsent = async (button: 'yes' | 'no') => {
    if (!consentData) return;

    const inputModel: ConsentInputModel = {
      button: button === 'yes' ? 'yes' : 'no',
      scopesConsented: button === 'yes' ? Array.from(selectedScopes) : [],
      returnUrl: returnUrl || '',
      rememberConsent: rememberChoice
    };

    try {
      const response = await fetch(`${config.API_URL}/consent/save`, {
        method: 'POST',
        credentials: 'include',
        headers: {
          'Content-Type': 'application/json'
        },
        body: JSON.stringify(inputModel)
      });

      if (response.ok) {
        const data = await response.json();
        window.location.href = data.returnUrl;
      } else {
        const errorData = await response.json();
        setError(errorData.error || 'Failed to save consent');
      }
    } catch (err) {
      setError('An unexpected error occurred while saving consent');
    }
  };

  const handleBack = () => {
    navigate(-1);
  };

  if (loading) {
    return <div>Loading...</div>;
  }

  if (error || !returnUrl) {
    return <div className="error-message">{error || 'returnUrl parameter is required'}</div>;
  }

  return (
    <div className="consent-container">
      {consentData && (
        <>
          <div className="client-info">
            {consentData.clientLogoUrl && (
              <img
                src={consentData.clientLogoUrl}
                alt={`${consentData.clientName} logo`}
                className="client-logo"
              />
            )}
            <h2>{consentData.clientName}</h2>
            {consentData.clientUrl && (
              <a href={consentData.clientUrl} target="_blank" rel="noopener noreferrer">
                {consentData.clientUrl}
              </a>
            )}
          </div>

          <form onSubmit={(e) => e.preventDefault()}>
            {consentData.identityScopes.length > 0 && (
              <div className="scope-section">
                <h3>Personal Information</h3>
                {consentData.identityScopes.map(scope => (
                  <div key={scope.name} className={`scope-item${scope.emphasize ? '-emph' : ''}`}>
                    <label>
                      <input
                        type="checkbox"
                        checked={selectedScopes.has(scope.value)}
                        disabled={scope.required}
                        onChange={(e) => handleScopeChange(scope.value, e.target.checked)}
                      />
                      <strong>{scope.displayName}</strong>
                      {scope.description && <p>{scope.description}</p>}
                    </label>
                  </div>
                ))}
              </div>
            )}

            {consentData.apiScopes.length > 0 && (
              <div className="scope-section">
                <h3>Application Access</h3>
                {consentData.apiScopes.map(scope => (
                  <div key={scope.name} className="scope-item">
                    <label>
                      <input
                        type="checkbox"
                        checked={selectedScopes.has(scope.value)}
                        disabled={scope.required}
                        onChange={(e) => handleScopeChange(scope.value, e.target.checked)}
                      />
                      <strong>{scope.displayName}</strong>
                      {scope.description && <p>{scope.description}</p>}
                    </label>
                  </div>
                ))}
              </div>
            )}

            {consentData.allowRememberConsent && (
              <div className="remember-consent">
                <label>
                  <input
                    type="checkbox"
                    checked={rememberChoice}
                    onChange={(e) => setRememberChoice(e.target.checked)}
                  />
                  Remember My Choice
                </label>
              </div>
            )}

            {error && (
              <div className="error-message">
                {error}
              </div>
            )}

            <div className="button-group">
              <button type="button" onClick={handleBack}>
                Back
              </button>
              <button
                type="button"
                className="primary"
                onClick={() => handleConsent('yes')}
              >
                Yes, Allow
              </button>
              <button
                type="button"
                className="secondary"
                onClick={() => handleConsent('no')}
              >
                No, Do Not Allow
              </button>
            </div>
          </form>
        </>
      )}
    </div>
  );
};

export default Consent;