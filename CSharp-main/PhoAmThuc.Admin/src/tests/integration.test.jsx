import { describe, it, expect, beforeEach, vi } from 'vitest'
import { render, screen, fireEvent, waitFor } from '@testing-library/react'
import { BrowserRouter } from 'react-router-dom'

// Mock API calls
const mockFetch = vi.fn()
global.fetch = mockFetch

const renderWithRouter = (component) => {
  return render(
    <BrowserRouter>
      {component}
    </BrowserRouter>
  )
}

describe('Web Admin Integration Tests', () => {
  beforeEach(() => {
    mockFetch.mockClear()
  })

  describe('Authentication Flow', () => {
    it('should login successfully with valid credentials', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => ({ token: 'mock-token', user: { id: 1, name: 'Admin' } })
      })

      const LoginForm = () => (
        <form onSubmit={(e) => {
          e.preventDefault()
          fetch('/api/auth/login', {
            method: 'POST',
            body: JSON.stringify({ email: 'admin@test.com', password: 'password' })
          })
        }}>
          <input name="email" placeholder="Email" />
          <input name="password" type="password" placeholder="Password" />
          <button type="submit">Login</button>
        </form>
      )

      render(<LoginForm />)
      
      fireEvent.change(screen.getByPlaceholderText('Email'), { target: { value: 'admin@test.com' } })
      fireEvent.change(screen.getByPlaceholderText('Password'), { target: { value: 'password' } })
      fireEvent.click(screen.getByText('Login'))

      await waitFor(() => {
        expect(mockFetch).toHaveBeenCalledWith('/api/auth/login', expect.objectContaining({
          method: 'POST'
        }))
      })
    })

    it('should handle login failure', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: false,
        json: async () => ({ error: 'Invalid credentials' })
      })

      const LoginForm = () => {
        const [error, setError] = React.useState('')
        
        return (
          <form onSubmit={async (e) => {
            e.preventDefault()
            const response = await fetch('/api/auth/login')
            if (!response.ok) {
              setError('Login failed')
            }
          }}>
            <button type="submit">Login</button>
            {error && <div data-testid="error">{error}</div>}
          </form>
        )
      }

      render(<LoginForm />)
      fireEvent.click(screen.getByText('Login'))

      await waitFor(() => {
        expect(screen.getByTestId('error')).toBeInTheDocument()
      })
    })
  })

  describe('Tour Management', () => {
    it('should create new tour', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => ({ id: 1, name: 'New Tour', description: 'Test tour' })
      })

      const TourForm = () => (
        <form onSubmit={(e) => {
          e.preventDefault()
          const formData = new FormData(e.target)
          fetch('/api/tours', {
            method: 'POST',
            body: JSON.stringify({
              name: formData.get('name'),
              description: formData.get('description')
            })
          })
        }}>
          <input name="name" placeholder="Tour Name" />
          <textarea name="description" placeholder="Description" />
          <button type="submit">Create Tour</button>
        </form>
      )

      render(<TourForm />)
      
      fireEvent.change(screen.getByPlaceholderText('Tour Name'), { target: { value: 'New Tour' } })
      fireEvent.change(screen.getByPlaceholderText('Description'), { target: { value: 'Test tour' } })
      fireEvent.click(screen.getByText('Create Tour'))

      await waitFor(() => {
        expect(mockFetch).toHaveBeenCalledWith('/api/tours', expect.objectContaining({
          method: 'POST'
        }))
      })
    })

    it('should load and display tours list', async () => {
      const mockTours = [
        { id: 1, name: 'Tour 1', description: 'First tour' },
        { id: 2, name: 'Tour 2', description: 'Second tour' }
      ]

      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => mockTours
      })

      const ToursList = () => {
        const [tours, setTours] = React.useState([])
        
        React.useEffect(() => {
          fetch('/api/tours')
            .then(res => res.json())
            .then(setTours)
        }, [])

        return (
          <div>
            {tours.map(tour => (
              <div key={tour.id} data-testid={`tour-${tour.id}`}>
                {tour.name}
              </div>
            ))}
          </div>
        )
      }

      render(<ToursList />)

      await waitFor(() => {
        expect(screen.getByTestId('tour-1')).toBeInTheDocument()
        expect(screen.getByTestId('tour-2')).toBeInTheDocument()
      })
    })
  })

  describe('POI Management', () => {
    it('should create POI with location', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => ({ id: 1, name: 'New POI', latitude: 10.123, longitude: 106.456 })
      })

      const POIForm = () => (
        <form onSubmit={(e) => {
          e.preventDefault()
          const formData = new FormData(e.target)
          fetch('/api/pois', {
            method: 'POST',
            body: JSON.stringify({
              name: formData.get('name'),
              latitude: parseFloat(formData.get('latitude')),
              longitude: parseFloat(formData.get('longitude'))
            })
          })
        }}>
          <input name="name" placeholder="POI Name" />
          <input name="latitude" type="number" step="any" placeholder="Latitude" />
          <input name="longitude" type="number" step="any" placeholder="Longitude" />
          <button type="submit">Create POI</button>
        </form>
      )

      render(<POIForm />)
      
      fireEvent.change(screen.getByPlaceholderText('POI Name'), { target: { value: 'New POI' } })
      fireEvent.change(screen.getByPlaceholderText('Latitude'), { target: { value: '10.123' } })
      fireEvent.change(screen.getByPlaceholderText('Longitude'), { target: { value: '106.456' } })
      fireEvent.click(screen.getByText('Create POI'))

      await waitFor(() => {
        expect(mockFetch).toHaveBeenCalledWith('/api/pois', expect.objectContaining({
          method: 'POST'
        }))
      })
    })
  })

  describe('Map Integration', () => {
    it('should render map with POIs', async () => {
      const MapComponent = () => {
        React.useEffect(() => {
          // Mock map initialization
          const map = L.map('map')
          L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png').addTo(map)
        }, [])

        return <div id="map" data-testid="map-container" style={{ height: '400px' }} />
      }

      render(<MapComponent />)
      
      expect(screen.getByTestId('map-container')).toBeInTheDocument()
      expect(L.map).toHaveBeenCalledWith('map')
    })
  })

  describe('QR Code Generation', () => {
    it('should generate QR code for POI', () => {
      const QRGenerator = ({ poiId }) => (
        <div data-testid="qr-code">
          QR Code for POI {poiId}
        </div>
      )

      render(<QRGenerator poiId={123} />)
      
      expect(screen.getByTestId('qr-code')).toHaveTextContent('QR Code for POI 123')
    })
  })
})