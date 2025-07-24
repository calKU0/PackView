# PackView

**PackView** is a .NET WPF application designed to monitor and display video footage from document packing processes. It visualizes product-level packaging events by retrieving and presenting camera recordings from specific packing stations, paired with product and operator metadata.

## Features

- 🎥 **Camera Footage Playback**:
  - Displays video recordings of individual product packing moments.
  - Allows timeline-based playback navigation using **LibVLC**.
- 🗃️ **Product Metadata Panel**:
  - Shows detailed info per packed product: name, code, image, packing quantity, packed date, and operator.
- 🧩 **ERP Integration (Hydra Addon)**:
  - Triggered from Comarch ERP XL to preview the packaging process directly from a document.
  - Launches WPF app with parameters for packed products selected by the user.
- 📥 **Recording Download & Storage**:
  - Connects to Hikvision cameras over **RTSP**.
  - Downloads video segments via **LibVLC** and saves them temporarily.
  - Users can move downloaded videos to a selected folder.
- 🖥️ **Interactive UI**:
  - Enables playback control over saved footage.

## Technologies Used

- **.NET WPF** – Main application and UI
- **LibVLC** – Video playback with timeline navigation
- **RTSP / Hikvision** – Camera stream retrieval
- **ERP Hydra Addon** – Integration with Comarch ERP XL

## License

This project is proprietary and confidential. See the [LICENSE](LICENSE) file for more information.

---

© 2025 [calKU0](https://github.com/calKU0)
