import { Injectable } from '@angular/core';
import { Observable, throwError } from 'rxjs';
import { map, catchError } from 'rxjs/operators';
import { ApiService } from './api.service';
import { Order, CreateOrderRequest, OrderTracking, OrderFilter, OrderStatus } from '../models/order.model';

@Injectable({
  providedIn: 'root'
})
export class OrderService {
  constructor(private apiService: ApiService) { }

  // Create new order
  createOrder(orderData: CreateOrderRequest): Observable<Order> {
    return this.apiService.post<Order>('orders', orderData).pipe(
      catchError(error => {
        console.error('Create order error:', error);
        return throwError(() => error);
      })
    );
  }

  // Get all orders
  getOrders(filter?: OrderFilter): Observable<{ orders: Order[], totalCount: number }> {
    let params = new URLSearchParams();
    
    if (filter) {
      if (filter.status) params.append('status', filter.status);
      if (filter.priority) params.append('priority', filter.priority);
      if (filter.serviceType) params.append('serviceType', filter.serviceType);
      if (filter.dateFrom) params.append('dateFrom', filter.dateFrom.toISOString());
      if (filter.dateTo) params.append('dateTo', filter.dateTo.toISOString());
      if (filter.customerId) params.append('customerId', filter.customerId.toString());
      if (filter.driverId) params.append('driverId', filter.driverId.toString());
      if (filter.page) params.append('page', filter.page.toString());
      if (filter.pageSize) params.append('pageSize', filter.pageSize.toString());
    }

    return this.apiService.get<{ orders: Order[], totalCount: number }>(`orders?${params.toString()}`).pipe(
      catchError(error => {
        console.error('Get orders error:', error);
        return throwError(() => error);
      })
    );
  }

  // Get order by ID
  getOrderById(id: number): Observable<Order> {
    return this.apiService.get<Order>(`orders/${id}`).pipe(
      catchError(error => {
        console.error('Get order error:', error);
        return throwError(() => error);
      })
    );
  }

  // Update order
  updateOrder(id: number, orderData: Partial<Order>): Observable<Order> {
    return this.apiService.put<Order>(`orders/${id}`, orderData).pipe(
      catchError(error => {
        console.error('Update order error:', error);
        return throwError(() => error);
      })
    );
  }

  // Delete order
  deleteOrder(id: number): Observable<any> {
    return this.apiService.delete(`orders/${id}`).pipe(
      catchError(error => {
        console.error('Delete order error:', error);
        return throwError(() => error);
      })
    );
  }

  // Assign driver to order
  assignDriver(orderId: number, driverId: number): Observable<Order> {
    return this.apiService.post<Order>(`orders/${orderId}/assign`, { driverId }).pipe(
      catchError(error => {
        console.error('Assign driver error:', error);
        return throwError(() => error);
      })
    );
  }

  // Update order status
  updateOrderStatus(orderId: number, status: OrderStatus, notes?: string): Observable<Order> {
    return this.apiService.put<Order>(`orders/${orderId}/status`, { status, notes }).pipe(
      catchError(error => {
        console.error('Update order status error:', error);
        return throwError(() => error);
      })
    );
  }

  // Track order
  trackOrder(orderId: number): Observable<OrderTracking> {
    return this.apiService.get<OrderTracking>(`orders/${orderId}/track`).pipe(
      catchError(error => {
        console.error('Track order error:', error);
        return throwError(() => error);
      })
    );
  }

  // Get orders by customer
  getOrdersByCustomer(customerId: number, filter?: OrderFilter): Observable<{ orders: Order[], totalCount: number }> {
    const orderFilter = { ...filter, customerId };
    return this.getOrders(orderFilter);
  }

  // Get orders by driver
  getOrdersByDriver(driverId: number, filter?: OrderFilter): Observable<{ orders: Order[], totalCount: number }> {
    const orderFilter = { ...filter, driverId };
    return this.getOrders(orderFilter);
  }

  // Get available orders for driver
  getAvailableOrders(driverId: number): Observable<Order[]> {
    return this.apiService.get<Order[]>(`orders/available/${driverId}`).pipe(
      catchError(error => {
        console.error('Get available orders error:', error);
        return throwError(() => error);
      })
    );
  }

  // Accept order (for drivers)
  acceptOrder(orderId: number, driverId: number): Observable<Order> {
    return this.apiService.post<Order>(`orders/${orderId}/accept`, { driverId }).pipe(
      catchError(error => {
        console.error('Accept order error:', error);
        return throwError(() => error);
      })
    );
  }

  // Complete order
  completeOrder(orderId: number, actualCost?: number, notes?: string): Observable<Order> {
    return this.apiService.post<Order>(`orders/${orderId}/complete`, { actualCost, notes }).pipe(
      catchError(error => {
        console.error('Complete order error:', error);
        return throwError(() => error);
      })
    );
  }

  // Cancel order
  cancelOrder(orderId: number, reason: string): Observable<Order> {
    return this.apiService.post<Order>(`orders/${orderId}/cancel`, { reason }).pipe(
      catchError(error => {
        console.error('Cancel order error:', error);
        return throwError(() => error);
      })
    );
  }

  // Get order statistics
  getOrderStatistics(): Observable<any> {
    return this.apiService.get<any>('orders/statistics').pipe(
      catchError(error => {
        console.error('Get order statistics error:', error);
        return throwError(() => error);
      })
    );
  }
}