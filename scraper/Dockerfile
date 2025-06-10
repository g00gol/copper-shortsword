FROM debian:bullseye-slim

RUN apt-get update && apt-get install -y \
  unzip wget curl ca-certificates libgl1-mesa-glx libxext6 \
  libxrandr2 libxss1 libxcursor1 libxi6 libasound2 libpulse0 \
  libxcomposite1 libxdamage1 libxtst6 libgtk-3-0 mono-complete \
  && apt-get clean && rm -rf /var/lib/apt/lists/*

WORKDIR /tmod

RUN wget https://github.com/tModLoader/tModLoader/releases/download/v2025.04.3.0/tModLoader.zip \
  && unzip tModLoader.zip -d . \
  && rm tModLoader.zip

COPY enabled.json /tmod/ModLoader/enabled.json

COPY install_mod.sh /tmod/install_mod.sh
RUN chmod +x install_mod.sh && ./install_mod.sh

CMD ["./tModLoaderServer", "-nogui"]
