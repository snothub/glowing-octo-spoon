export interface LoginFormData {
  username: string;
  password: string;
  returnUrl: string;
}

export interface LoginResponse {
  returnUrl: string;
}

export interface LoginError {
  error: string;
}