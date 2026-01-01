import { Component, OnInit, OnDestroy } from '@angular/core';
import { Router, RouterOutlet, NavigationEnd } from '@angular/router';
import { filter, Subscription } from 'rxjs';
import { HeaderComponent } from './components/header/header.component';
import { FooterComponent } from './components/footer/footer.component';

declare let gtag: Function;

@Component({
  selector: 'app-root',
  imports: [
    RouterOutlet, 
    HeaderComponent,
    FooterComponent
  ],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss'
})
export class AppComponent implements OnInit, OnDestroy {
  title = 'Strong Towing Services';
  private routerSubscription: Subscription | undefined;
  showHeaderFooter = true;
  currentRoute = '';

  constructor(private router: Router) {}

  ngOnInit() {
    // Check initial route
    this.currentRoute = this.router.url;
    this.updateHeaderFooterVisibility();

    // Track route changes for GTM and header/footer visibility
    this.routerSubscription = this.router.events
      .pipe(filter(event => event instanceof NavigationEnd))
      .subscribe((event: any) => {
        this.currentRoute = event.urlAfterRedirects;
        this.updateHeaderFooterVisibility();
        
        if (typeof gtag !== 'undefined') {
          gtag('config', 'G-TDTV6MTD42', {
            page_path: event.urlAfterRedirects
          });
        }
      });
  }

  private updateHeaderFooterVisibility(): void {
    // Hide header and footer on dashboard routes
    const dashboardRoutes = ['/admin', '/driver', '/customer'];
    this.showHeaderFooter = !dashboardRoutes.some(route => 
      this.currentRoute.startsWith(route)
    );
  }

  ngOnDestroy() {
    if (this.routerSubscription) {
      this.routerSubscription.unsubscribe();
    }
  }
}
