const { files} = require("../data/mockDb");

exports.getAllFiles = () => {
    return files;
};

exports.getFileById = (fileId) => {
    return files.find(f => f.id === fileId);
};

exports.getUserFiles = (user) => {
    if (user.role === 'admin') {
        return files;
    }

    return files.filter(file => file.ownerId === user.id);
};

exports.addFile = (fileData) => {
    files.push(fileData);
    return fileData;
};

exports.deleteFile = (fileId, user) => {
  const index = files.findIndex(f => f.id === fileId);

  if (index === -1) return null;

  const file = files[index];

  if (user.role !== "admin" && file.ownerId !== user.id) {
    return "forbidden";
  }

  files.splice(index, 1);
  return file;
};

exports.canAccessFile = (file, user) => {
    if (!file) return false;

    if (user.role === "admin") return true;

    return file.ownerId === user.id;
};
