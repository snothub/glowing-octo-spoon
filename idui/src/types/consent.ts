interface ResourceViewModel {
  name: string;
  displayName: string;
}

interface ScopeViewModel {
  name: string;
  value: string;
  displayName: string;
  description: string;
  emphasize: boolean;
  required: boolean;
  checked: boolean;
  resources: ResourceViewModel[];
}

export interface ConsentViewModel {
  clientName: string;
  clientUrl: string;
  clientLogoUrl: string;
  allowRememberConsent: boolean;
  identityScopes: ScopeViewModel[];
  apiScopes: ScopeViewModel[];
}

export interface ConsentInputModel {
  button: string;
  scopesConsented: string[];
  rememberConsent: boolean;
  id?: string;
  returnUrl: string;
  description?: string;
}