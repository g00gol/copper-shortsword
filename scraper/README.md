# Terraria Item Data Scraper

This project uses tModLoader to extract comprehensive item data from Terraria, including sprites, tooltips, recipes, and metadata. The scraper runs in a Docker container with a dedicated tModLoader server to process game data.

## Quick Start

### Prerequisites

- Docker and Docker Compose installed
- At least 4GB of available disk space
- Port 7777 available (or modify the port mapping)

### Running the Scraper

1. **Build and start the container:**

   ```bash
   docker-compose up --build
   ```

2. **Access the container (if needed):**

   ```bash
   docker-compose exec tml bash
   ```

3. **Stop the container:**
   ```bash
   docker-compose down
   ```

## Configuration

### Docker Compose Options

The `docker-compose.yml` file includes several configuration options:

- **UID/GID**: Set to match your local user (default: 1000) to avoid permission issues
- **TML_VERSION**: Uncomment and specify a specific tModLoader version if needed
- **Port mapping**: Change `7777:7777` if you need a different port
- **Debug mode**: Uncomment the `entrypoint` line to access the container shell

### Environment Variables

You can customize the build with these arguments in `docker-compose.yml`:

```yaml
build:
  args:
    UID: 1000 # Your user ID
    GID: 1000 # Your group ID
    TML_VERSION: v2023.8.3.3 # Specific tModLoader version
```

## Project Structure

```
scraper/
├── Dockerfile              # Container setup with tModLoader
├── docker-compose.yml      # Service configuration
├── src/                    # C# mod source code
│   ├── ItemDataExporter.cs # Main mod class
│   ├── ItemDataExporter.csproj
│   ├── Models/             # Data models
│   ├── Services/           # Processing services
│   └── Exporters/          # Output formatters
└── tModLoader/             # tModLoader data directory
    ├── Mods/               # Installed mods
    ├── Worlds/             # World files
    ├── ItemDataExport/     # Generated data output
    └── logs/               # Server logs
```

## Testing and Debugging

### Basic Health Checks

1. **Verify container is running:**

   ```bash
   docker-compose ps
   ```

2. **Check tModLoader server logs:**

   ```bash
   docker-compose logs -f tml
   ```

3. **View server logs directly:**
   ```bash
   tail -f tModLoader/logs/server.log
   ```

### Debug Mode Access

To explore the container and debug issues:

1. **Enable debug mode** by uncommenting this line in `docker-compose.yml`:

   ```yaml
   entrypoint: ["/bin/bash"]
   ```

2. **Rebuild and access the container:**

   ```bash
   docker-compose up --build
   docker-compose exec tml bash
   ```

3. **Inside the container, you can:**

   ```bash
   # Check tModLoader installation
   ls -la ~/.local/share/Terraria/tModLoader/

   # Run the server manually
   ./manage-tModLoaderServer.sh start

   # View mod status
   cat ~/.local/share/Terraria/tModLoader/Mods/enabled.json
   ```

### Testing the Mod

1. **Check if the mod is loaded:**

   ```bash
   # Look for ItemDataExporter in the mod list
   grep -i "itemdata" tModLoader/logs/server.log
   ```

2. **Verify world creation:**

   ```bash
   # Check if the dummy world exists
   ls -la tModLoader/Worlds/
   ```

3. **Monitor data export:**
   ```bash
   # Watch for exported data
   ls -la tModLoader/ItemDataExport/
   find tModLoader/ItemDataExport/ -name "*.json" -exec wc -l {} \;
   ```

## Development Workflow

1. **Make changes to C# code** in the `src/` directory
2. **Compile the mod locally:**
   - Open tModLoader on your local machine
   - Navigate to the mod sources in the tModLoader workshop
   - Copy the `scraper/src/` directory contents to your local tModLoader mod development folder
   - Use tModLoader's built-in mod compilation tools to build the mod
   - The compiled `.tmod` file should be copied into the `scraper/tModLoader/Mods/` directory
   - For questions, refer to [tModLoader documentation here](https://github.com/tModLoader/tModLoader/wiki/Basic-tModLoader-Modding-Guide)
3. **Rebuild the container** to run with the compiled mod:
   ```bash
   docker-compose down
   docker-compose up --build
   ```
4. **Monitor logs** for compilation errors or runtime issues
5. **Check output** in `tModLoader/ItemDataExport/`

## Data Output

The scraper exports data to `tModLoader/ItemDataExport/`:

- `sprites/` - Item sprite images
- JSON files with item data, recipes, tooltips, etc.

## Troubleshooting

If you encounter issues, check these in order:

1. Docker container logs
2. tModLoader server logs
3. Mod compilation status
4. File permissions
5. Available disk space
6. Network connectivity (for Steam/mod downloads)
