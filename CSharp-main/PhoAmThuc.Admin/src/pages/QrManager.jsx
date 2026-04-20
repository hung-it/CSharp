import { useEffect, useMemo, useRef, useState } from 'react';
import { Download, Printer, Copy, QrCode, RefreshCw } from 'lucide-react';
import { QRCodeCanvas } from 'qrcode.react';
import { apiGet } from '../services/apiClient';

export default function QrManager() {
  const qrRef = useRef(null);
  const [pois, setPois] = useState([]);
  const [selectedPoiId, setSelectedPoiId] = useState('');
  const [languageCode, setLanguageCode] = useState('vi');
  const [error, setError] = useState('');
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    let active = true;

    async function loadPois() {
      setIsLoading(true);
      setError('');
      try {
        const poiData = await apiGet('/pois');
        if (!active) {
          return;
        }

        const safePois = Array.isArray(poiData) ? poiData : [];
        setPois(safePois);
        setSelectedPoiId(safePois[0]?.id || '');
      } catch (loadError) {
        if (active) {
          setError(loadError.message || 'Không thể tải danh sách POI.');
        }
      } finally {
        if (active) {
          setIsLoading(false);
        }
      }
    }

    loadPois();

    return () => {
      active = false;
    };
  }, []);

  const selectedPoi = useMemo(
    () => pois.find((poi) => poi.id === selectedPoiId) || null,
    [pois, selectedPoiId]
  );

  const qrPayload = useMemo(() => {
    if (!selectedPoi?.code) {
      return '';
    }

    return `QR:${selectedPoi.code}`;
  }, [selectedPoi]);

  async function copyPayload() {
    if (!qrPayload) {
      return;
    }

    try {
      await navigator.clipboard.writeText(qrPayload);
    } catch {
      setError('Không thể sao chép payload vào clipboard.');
    }
  }

  function downloadQrPng() {
    const canvas = qrRef.current?.querySelector('canvas');
    if (!canvas || !selectedPoi) {
      return;
    }

    const link = document.createElement('a');
    link.download = `qr-${selectedPoi.code}.png`;
    link.href = canvas.toDataURL('image/png');
    link.click();
  }

  function printQr() {
    const canvas = qrRef.current?.querySelector('canvas');
    if (!canvas || !selectedPoi) {
      return;
    }

    const imageData = canvas.toDataURL('image/png');
    const printWindow = window.open('', '_blank', 'width=600,height=700');
    if (!printWindow) {
      setError('Trình duyệt đã chặn cửa sổ in.');
      return;
    }

    printWindow.document.write(`
      <html>
        <head>
          <title>QR ${selectedPoi.code}</title>
          <style>
            body { font-family: Arial, sans-serif; text-align: center; padding: 24px; }
            img { width: 320px; height: 320px; }
            .meta { margin-top: 16px; font-size: 14px; }
          </style>
        </head>
        <body>
          <h2>QR điểm dừng ${selectedPoi.name}</h2>
          <img src="${imageData}" alt="QR ${selectedPoi.code}" />
          <div class="meta">Payload: ${qrPayload}</div>
          <div class="meta">Ngôn ngữ gợi ý: ${languageCode}</div>
        </body>
      </html>
    `);
    printWindow.document.close();
    printWindow.focus();
    printWindow.print();
  }

  function exportPayloadList() {
    const rows = pois.map((poi) => {
      const payload = `QR:${poi.code}`;
      return [poi.code, poi.name, payload].map((item) => `"${String(item).replaceAll('"', '""')}"`).join(',');
    });

    const csv = ['"poiCode","poiName","qrPayload"', ...rows].join('\n');
    const blob = new Blob([csv], { type: 'text/csv;charset=utf-8;' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = 'qr-payloads.csv';
    link.click();
    URL.revokeObjectURL(url);
  }

  return (
    <div className='space-y-6 max-w-7xl mx-auto'>
      <div className='bg-white p-5 rounded-2xl shadow-sm border border-pink-100'>
        <h1 className='text-2xl font-bold text-transparent bg-clip-text bg-linear-to-r from-pink-600 to-purple-500'>
          QR Manager
        </h1>
        <p className='text-sm text-pink-400/90 mt-1'>
          Tạo payload QR theo POI, tải PNG, in tem QR và export danh sách payload.
        </p>
      </div>

      {error && (
        <div className='rounded-xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-700'>
          {error}
        </div>
      )}

      <div className='grid grid-cols-1 lg:grid-cols-3 gap-6'>
        <div className='lg:col-span-2 bg-white rounded-2xl border border-pink-100 p-5 shadow-sm space-y-4'>
          <div className='grid grid-cols-1 md:grid-cols-2 gap-3'>
            <label className='text-sm text-gray-600'>
              Chọn POI
              <select
                className='mt-1 w-full rounded-xl border border-pink-100 bg-pink-50/60 px-3 py-2'
                value={selectedPoiId}
                onChange={(event) => setSelectedPoiId(event.target.value)}
                disabled={isLoading}
              >
                {pois.map((poi) => (
                  <option key={poi.id} value={poi.id}>
                    {poi.name} ({poi.code})
                  </option>
                ))}
              </select>
            </label>

            <label className='text-sm text-gray-600'>
              Ngôn ngữ gợi ý
              <input
                className='mt-1 w-full rounded-xl border border-pink-100 px-3 py-2'
                value={languageCode}
                onChange={(event) => setLanguageCode(event.target.value)}
                placeholder='vi / en / ko...'
              />
            </label>
          </div>

          <div className='rounded-xl border border-pink-100 bg-pink-50/40 p-4'>
            <div className='text-xs text-gray-500'>Payload dùng cho app quét</div>
            <div className='font-mono text-sm text-gray-800 mt-1 break-all'>
              {qrPayload || 'Chưa chọn POI'}
            </div>
          </div>

          <div className='flex flex-wrap gap-2'>
            <ActionButton icon={<Copy size={16} />} label='Copy payload' onClick={copyPayload} disabled={!qrPayload} />
            <ActionButton icon={<Download size={16} />} label='Export PNG' onClick={downloadQrPng} disabled={!qrPayload} />
            <ActionButton icon={<Printer size={16} />} label='In QR' onClick={printQr} disabled={!qrPayload} />
            <ActionButton icon={<RefreshCw size={16} />} label='Export CSV payload' onClick={exportPayloadList} disabled={pois.length === 0} />
          </div>
        </div>

        <div className='bg-white rounded-2xl border border-pink-100 p-5 shadow-sm'>
          <div className='flex items-center gap-2 text-pink-700 font-semibold'>
            <QrCode size={18} />
            Xem trước QR
          </div>
          <div className='mt-4 flex items-center justify-center rounded-xl border border-dashed border-pink-200 p-4 min-h-80' ref={qrRef}>
            {qrPayload ? (
              <QRCodeCanvas
                value={qrPayload}
                size={280}
                level='H'
                includeMargin
                bgColor='#ffffff'
                fgColor='#111827'
              />
            ) : (
              <div className='text-sm text-gray-500'>Chọn POI để tạo QR.</div>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}

function ActionButton({ icon, label, onClick, disabled }) {
  return (
    <button
      type='button'
      onClick={onClick}
      disabled={disabled}
      className='inline-flex items-center gap-2 rounded-lg border border-pink-200 bg-pink-50 px-3 py-2 text-sm font-medium text-pink-700 disabled:opacity-50'
    >
      {icon}
      {label}
    </button>
  );
}
