#!/bin/bash

echo "Installing ItemScraper mod from source..."

mkdir -p ModLoader/Mods/ModSources
cp -r ItemScraper ModLoader/Mods/ModSources/

./tModLoaderServer -buildmod ItemScraper
