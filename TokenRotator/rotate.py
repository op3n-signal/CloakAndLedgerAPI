import boto3
import json
import subprocess
from datetime import datetime

def get_secret(secret_name, region_name="us-east-1"):
    # Create a Secrets Manager client
    session = boto3.session.Session()
    client = session.client(service_name="secretsmanager", region_name=region_name)

    try:
        response = client.get_secret_value(SecretId=secret_name)

        # If the secret is a string
        if "SecretString" in response:
            secret = response["SecretString"]
            return json.loads(secret)  # returns as a dict if JSON
        else:
            # If secret is binary
            return response["SecretBinary"]

    except Exception as e:
        print(f"Error retrieving secret: {e}")
        return None

def put_secret(secret_name, secret_value, region_name="us-east-1"):
    """
    Updates an existing AWS secret with a new value.
    secret_value can be a dict (JSON) or a string
    """
    client = boto3.client("secretsmanager", region_name=region_name)

    # Convert dict to JSON string if necessary
    if isinstance(secret_value, dict):
        secret_value = json.dumps(secret_value)

    try:
        response = client.put_secret_value(
            SecretId=secret_name,
            SecretString=secret_value
        )
        print(f"Secret updated successfully: {response['ARN']}")
        return response
    except Exception as e:
        print(f"Error updating secret: {e}")
        return None

def log_with_time(message: str):
    """Helper to print messages with timestamp"""
    ts = datetime.now().strftime("%Y-%m-%d %H:%M:%S")
    print(f"[{ts}] {message}")


if __name__ == "__main__":
    secret_name = "encompass/securitykeys"
    secret = get_secret(secret_name)

    #print(secret)

    # Extract individual secret values
    salt = secret.get("salt")
    signing = secret.get("signing")
    encryption = secret.get("encryption")
    user = secret.get("user")
    valid_days = secret.get("valid_days")

    # Build the docker command
    cmd = [
        "docker", "run", 
        "ghcr.io/candid-middleware-organization/plaid-lambda-function:latest", #todo huh
        salt, signing, encryption, user, valid_days
    ]

    # Run the container and capture output
    result = subprocess.run(cmd, capture_output=True, text=True)


    # Split the output into lines
    lines = result.stdout.splitlines()

    # Find the line(s) after "Token" and before "Will expire at"
    token_lines = []
    capture = False
    for line in lines:
        if line.strip() == "Token":
            capture = True
            continue
        if line.strip().startswith("Will expire at"):
            capture = False
        if capture:
            token_lines.append(line.strip())

    # Join multi-line token if needed
    token = "".join(token_lines)

    log_with_time("New Token Generated")
    log_with_time("Token valid for days:" +valid_days )
    # Wrap token in a dict so Secrets Manager stores it as JSON
    secret_payload = {"jwt_token": token, "retrieveDisclosuresEndpoint": ""} #todo put endpoint

    put_secret("encompassMiddleware", secret_payload)
    log_with_time("AWS Secret Updated")


    if result.stderr:
        print("===== Docker Errors =====")
        print(result.stderr)

    # Optionally check exit code
    if result.returncode != 0:
        print(f"Docker container exited with code {result.returncode}")
