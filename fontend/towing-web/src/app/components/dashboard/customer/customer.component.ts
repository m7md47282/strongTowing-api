import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { OrderService } from '../../../services/order.service';
import { Order } from '../../../models/order.model';

@Component({
  selector: 'app-customer',
  imports: [CommonModule, RouterModule],
  templateUrl: './customer.component.html',
  styleUrls: ['./customer.component.scss']
})
export class CustomerComponent implements OnInit {
  totalOrders = 0;
  pendingOrders = 0;
  completedOrders = 0;
  totalSpent = 0;
  recentOrders: Order[] = [];

  constructor(private orderService: OrderService) { }

  ngOnInit(): void {
    this.loadDashboardData();
  }

  loadDashboardData(): void {
    // Load order statistics
    this.orderService.getOrderStatistics().subscribe({
      next: (stats) => {
        this.totalOrders = stats.totalOrders || 0;
        this.pendingOrders = stats.pendingOrders || 0;
        this.completedOrders = stats.completedOrders || 0;
      },
      error: (error) => {
        console.error('Error loading dashboard stats:', error);
      }
    });

    // Load recent orders
    this.orderService.getOrders({ page: 1, pageSize: 5 }).subscribe({
      next: (response) => {
        this.recentOrders = response.orders || [];
        this.calculateTotalSpent();
      },
      error: (error) => {
        console.error('Error loading recent orders:', error);
      }
    });
  }

  calculateTotalSpent(): void {
    this.totalSpent = this.recentOrders.reduce((total, order) => {
      return total + (order.actualCost || order.estimatedCost || 0);
    }, 0);
  }

  getStatusClass(status: string): string {
    switch (status.toLowerCase()) {
      case 'pending':
        return 'status-pending';
      case 'assigned':
        return 'status-assigned';
      case 'inprogress':
        return 'status-in-progress';
      case 'completed':
        return 'status-completed';
      case 'cancelled':
        return 'status-cancelled';
      default:
        return 'status-pending';
    }
  }

  requestNewService(): void {
    // Navigate to services page or open service request modal
    window.location.href = '/services';
  }

  trackOrder(): void {
    // Navigate to order tracking page
    window.location.href = '/track-order';
  }

  viewHistory(): void {
    // Navigate to order history page
    window.location.href = '/orders';
  }

  updateProfile(): void {
    // Navigate to profile page
    window.location.href = '/profile';
  }

  viewOrderDetails(orderId: number): void {
    // Navigate to order details page
    window.location.href = `/orders/${orderId}`;
  }

  cancelOrder(orderId: number): void {
    if (confirm('Are you sure you want to cancel this order?')) {
      this.orderService.cancelOrder(orderId, 'Cancelled by customer').subscribe({
        next: (response) => {
          // Reload dashboard data
          this.loadDashboardData();
        },
        error: (error) => {
          console.error('Error cancelling order:', error);
          alert('Failed to cancel order. Please try again.');
        }
      });
    }
  }
}