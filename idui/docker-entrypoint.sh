#!/bin/sh
set -e

# Certificate paths
PFX_PATH="/app/aspnetcore-dev-cert.pfx"
CERT_DIR="/etc/nginx/certs"
CERT_PASSWORD="MyPw123"

# Create certificate directory if it doesn't exist
mkdir -p "$CERT_DIR"

# Check if PFX certificate exists
if [ -f "$PFX_PATH" ]; then
    echo "Converting PFX certificate to PEM format..."

    # Extract private key
    openssl pkcs12 -in "$PFX_PATH" -nocerts -nodes -out "$CERT_DIR/server.key" -passin pass:"$CERT_PASSWORD" 2>/dev/null || {
        echo "Warning: Could not extract private key from PFX certificate"
    }

    # Extract certificate
    openssl pkcs12 -in "$PFX_PATH" -clcerts -nokeys -out "$CERT_DIR/server.crt" -passin pass:"$CERT_PASSWORD" 2>/dev/null || {
        echo "Warning: Could not extract certificate from PFX certificate"
    }

    # Set proper permissions
    chmod 600 "$CERT_DIR/server.key"
    chmod 644 "$CERT_DIR/server.crt"

    echo "Certificate conversion completed successfully"
else
    echo "Warning: PFX certificate not found at $PFX_PATH"
    echo "HTTPS will not be available"
fi

# Start nginx
exec nginx -g "daemon off;"
