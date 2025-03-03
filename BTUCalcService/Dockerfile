FROM python:3.11-slim AS base

WORKDIR /app

ENV PYTHONUNBUFFERED=1 \
    TZ=Europe/Chisinau

RUN apt-get update && apt-get install -y \
    tzdata \
    && ln -snf /usr/share/zoneinfo/Europe/Chisinau /etc/localtime \
    && dpkg-reconfigure -f noninteractive tzdata \
    && apt-get clean \
    && rm -rf /var/lib/apt/lists/*

FROM base AS build

COPY BTUCalcService/requirements.txt /app/requirements.txt

RUN pip install --no-cache-dir -r requirements.txt

FROM base AS final

WORKDIR /app

COPY --from=build /usr/local/lib/python3.11 /usr/local/lib/python3.11

COPY BTUCalcService /app/

EXPOSE 8000

CMD ["python", "BTUCalcService.py"]
