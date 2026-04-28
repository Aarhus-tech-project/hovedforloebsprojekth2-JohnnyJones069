const express = require('express');
const multer = require('multer');
const router = express.Router();

const { authenticate, authorize } = require('../middleware/authMiddleware');
const fileController = require('../controllers/fileController');

const upload = multer({ dest: 'uploads/' });

router.get(
  "/admin/all",
  authenticate,
  authorize(["admin"]),
  fileController.getAllFiles
);

router.get('/', authenticate, fileController.getFiles);
router.post('/upload', authenticate, upload.single('file'), fileController.uploadFile);
router.delete('/:id', authenticate, fileController.deleteFile);
router.get('/download/:id', authenticate, fileController.downloadFile);

module.exports = router;
