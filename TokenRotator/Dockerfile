FROM docker:24.0.5-dind

# Install Python
RUN apk add --no-cache python3 py3-pip bash

WORKDIR /app


# Install Python dependencies
RUN pip install --no-cache-dir boto3

COPY TokenRotator/rotate.py .

# Copy crontab file
COPY TokenRotator/crontab.txt /etc/crontabs/root

# Run cron in foreground
CMD ["crond", "-f", "-L", "/dev/stdout"]

#CMD ["python3", "rotate.py"]