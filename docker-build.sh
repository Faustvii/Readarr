#!/usr/bin/env bash
set -e

# Default values
VERSION="0.4.19.0"
VENDOR="Faustvii"
BRANCH="develop"
IMAGE_NAME="readarr"
TAG="latest"

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --version)
            VERSION="$2"
            shift 2
            ;;
        --vendor)
            VENDOR="$2"
            shift 2
            ;;
        --branch)
            BRANCH="$2"
            shift 2
            ;;
        --image-name)
            IMAGE_NAME="$2"
            shift 2
            ;;
        --tag)
            TAG="$2"
            shift 2
            ;;
        --help)
            echo "Usage: $0 [OPTIONS]"
            echo "Options:"
            echo "  --version VERSION    Set the version (default: 0.4.19.0)"
            echo "  --vendor VENDOR      Set the vendor (default: Readarr)"
            echo "  --branch BRANCH      Set the branch (default: develop)"
            echo "  --image-name NAME    Set the image name (default: readarr)"
            echo "  --tag TAG           Set the tag (default: latest)"
            echo "  --help              Show this help message"
            exit 0
            ;;
        *)
            echo "Unknown option: $1"
            echo "Use --help for usage information"
            exit 1
            ;;
    esac
done

echo "Building Readarr Docker image..."
echo "Version: $VERSION"
echo "Vendor: $VENDOR"
echo "Branch: $BRANCH"
echo "Image: $IMAGE_NAME:$TAG"

# Build the Docker image
docker build \
    --build-arg VERSION="$VERSION" \
    --build-arg VENDOR="$VENDOR" \
    --build-arg BRANCH="$BRANCH" \
    --tag "$IMAGE_NAME:$TAG" \
    --tag "$IMAGE_NAME:$VERSION" \
    .

echo "Build completed successfully!"
echo "You can run the container with:"
echo "docker run -d --name readarr -p 8787:8787 -v /path/to/config:/config $IMAGE_NAME:$TAG" 