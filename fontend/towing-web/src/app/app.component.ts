import { Component, OnInit, OnDestroy } from '@angular/core';
import { Router, RouterOutlet, NavigationEnd } from '@angular/router';
import { filter, Subscription } from 'rxjs';
import { HeaderComponent } from './components/header/header.component';
import { FooterComponent } from './components/footer/footer.component';
import { AuthService } from './services/auth.service';

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
  private authSubscription: Subscription | undefined;
  showHeaderFooter = true;
  currentRoute = '';

  constructor(
    private router: Router,
    private authService: AuthService
  ) {}

  ngOnInit() {
    // Check initial route
    this.currentRoute = this.router.url;
    this.updateHeaderFooterVisibility();

    // Subscribe to authentication state changes
    this.authSubscription = this.authService.currentUser$.subscribe(() => {
      this.updateHeaderFooterVisibility();
    });

    // Track route changes for GTM
    this.routerSubscription = this.router.events
      .pipe(filter(event => event instanceof NavigationEnd))
      .subscribe((event: any) => {
        this.currentRoute = event.urlAfterRedirects;
        
        if (typeof gtag !== 'undefined') {
          gtag('config', 'G-TDTV6MTD42', {
            page_path: event.urlAfterRedirects
          });
        }
      });
  }

  private updateHeaderFooterVisibility(): void {
    // Hide header and footer when user is logged in
    this.showHeaderFooter = !this.authService.isAuthenticated();
  }

  ngOnDestroy() {
    if (this.routerSubscription) {
      this.routerSubscription.unsubscribe();
    }
    if (this.authSubscription) {
      this.authSubscription.unsubscribe();
    }
  }
}
