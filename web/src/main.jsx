import React, { useEffect, useState } from 'react';
import { createRoot } from 'react-dom/client';
import { BrowserRouter, Navigate, Route, Routes, useNavigate } from 'react-router-dom';
import './styles.css';

const SESSION_KEY = 'taspir-validated';

function App() {
  const [validated, setValidated] = useState(() => {
    try { return sessionStorage.getItem(SESSION_KEY) === 'yes'; }
    catch { return false; }
  });
  const navigate = useNavigate();

  function login() {
    try { sessionStorage.setItem(SESSION_KEY, 'yes'); } catch { /* Keep access in memory. */ }
    setValidated(true);
    navigate('/play', { replace: true });
  }

  function logout() {
    try { sessionStorage.removeItem(SESSION_KEY); } catch { /* Clear in-memory access below. */ }
    setValidated(false);
    navigate('/', { replace: true });
  }

  return (
    <Routes>
      <Route path="/" element={<Validation onSuccess={login} />} />
      <Route path="/play" element={validated ? <UnityPage onExit={logout} /> : <Navigate to="/" replace />} />
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
}

function Validation({ onSuccess }) {
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');

  function submit(event) {
    event.preventDefault();
    if (password === 'medok') onSuccess();
    else setError('Неверный пароль. Попробуйте ещё раз.');
  }

  return (
    <main className="validation-page">
      <form className="validation" onSubmit={submit} aria-labelledby="validation-title">
        <h1 id="validation-title">Пожалуйста, пройдите валидацию</h1>
        <input
          className="validation-input"
          type="password"
          name="password"
          aria-labelledby="validation-title"
          aria-invalid={Boolean(error)}
          aria-describedby={error ? 'validation-error' : undefined}
          placeholder="Введите пароль"
          autoComplete="current-password"
          value={password}
          onChange={(event) => { setPassword(event.target.value); setError(''); }}
          required
        />
        <button className="enter-button" type="submit">Войти</button>
        <p className="validation-error" id="validation-error" role="status">{error}</p>
      </form>
    </main>
  );
}

function UnityPage({ onExit }) {
  const [status, setStatus] = useState('loading');

  useEffect(() => {
    const controller = new AbortController();
    // Vite returns the React HTML for missing files, so check the Unity loader too.
    fetch('/unity/index.html', { signal: controller.signal, cache: 'no-cache' })
      .then(async (response) => {
        const html = await response.text();
        if (!response.ok || !html.includes('createUnityInstance')) throw new Error('Missing Unity build');
        setStatus('ready');
      })
      .catch((error) => { if (error.name !== 'AbortError') setStatus('missing'); });
    return () => controller.abort();
  }, []);

  return (
    <main className="unity-page">
      <header className="unity-header">
        <span>Taspir</span>
        <button className="exit-button" onClick={onExit}>Выйти</button>
      </header>
      {status === 'ready' ? (
        <iframe className="unity-frame" src="/unity/index.html" title="Taspir — Unity" allow="fullscreen; autoplay" allowFullScreen />
      ) : (
        <div className="unity-message" role="status">
          <h1>{status === 'loading' ? 'Подключаем приложение…' : 'Приложение скоро появится'}</h1>
          {status === 'missing' && <p>Вход выполнен. Unity-приложение пока не опубликовано.</p>}
        </div>
      )}
    </main>
  );
}

createRoot(document.getElementById('root')).render(
  <React.StrictMode><BrowserRouter><App /></BrowserRouter></React.StrictMode>,
);
