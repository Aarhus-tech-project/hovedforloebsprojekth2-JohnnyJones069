const jwt  = require('jsonwebtoken');
const { users } = require('../data/mockDb');

exports.login = (req, res) => {
  const { username, password } = req.body;

  const user = users.find(u => u.username === username && u.password === password);

  if (!user) {
    return res.status(401).json({ message: 'Invalid credentials' });
  }

  const token = jwt.sign(
    {
        id: user.id,
        username: user.username,
        role: user.role
    },
    process.env.JWT_SECRET,
    { expiresIn: '1h' }
  );
  res.json({ 
    message: 'Login successful',
    token,
    user: {
        id: user.id,
        username: user.username,
        role: user.role
    }
  });
};