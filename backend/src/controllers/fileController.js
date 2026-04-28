const fileService = require("../services/fileService");
const path = require("path");

exports.getFiles = (req, res) => {
  const result = fileService.getUserFiles(req.user);
  res.json(result);
};

exports.getAllFiles = (req, res) => {
  const result = fileService.getAllFiles();
  res.json(result);
};

exports.uploadFile = (req, res) => {
  if (!req.file) {
    return res.status(400).json({ message: "No file uploaded" });
  }

  const extension = req.file.originalname.split(".").pop().toLowerCase();
  const allowedTypes = ["pdf", "txt", "docx"];

  if (!allowedTypes.includes(extension)) {
    return res.status(400).json({ message: "File type not allowed" });
  }

  const fileData = {
    id: Date.now(), // simple unique id
    originalName: req.file.originalname,
    storedName: req.file.filename,
    type: extension,
    size: req.file.size,
    ownerId: req.user.id,
    metadata: {
      uploadedAt: new Date(),
      description: ""
    }
  };

  const newFile = fileService.addFile(fileData);

  res.status(201).json({
    message: "File uploaded",
    file: newFile
  });
};

exports.deleteFile = (req, res) => {
  const fileId = parseInt(req.params.id);

  const result = fileService.deleteFile(fileId, req.user);

  if (result === null) {
    return res.status(404).json({ message: "File not found" });
  }

  if (result === "forbidden") {
    return res.status(403).json({ message: "Not allowed" });
  }

  res.json({ message: "File deleted" });
};

exports.downloadFile = (req, res) => {
  const fileId = parseInt(req.params.id);

  const file = fileService.getFileById(fileId);

  if (!file) {
    return res.status(404).json({ message: "File not found" });
  }

  const canAccess = fileService.canAccessFile(file, req.user);

  if (!canAccess) {
    return res.status(403).json({ message: "Not allowed" });
  }

  const filePath = path.join(__dirname, "../../uploads", file.storedName);

  res.download(filePath, file.originalName);
};