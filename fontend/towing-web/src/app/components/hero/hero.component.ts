import { Component, OnInit, OnDestroy } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Splide } from '@splidejs/splide';

interface HeroSlide {
  title: string;
  subtitles: string[];
  price: string;
  priceDescription: string;
  backgroundImage: string;
}

@Component({
  selector: 'app-hero',
  imports: [CommonModule, FormsModule],
  templateUrl: './hero.component.html',
  styleUrls: ['./hero.component.scss']
})
export class HeroComponent implements OnInit, OnDestroy {
  searchTerm: string = '';
  private heroSlider: Splide | null = null;

  heroSlides: HeroSlide[] = [
    {
      title: 'TRUSTED TOWING SERVICES,<br />RIGHT WHEN YOU NEED THEM',
      subtitles: [
        'Skilled Towing Solutions for Every Situation',
        'Towing you can trust – fast, secure, and hassle-free'
      ],
      price: '75$',
      priceDescription: 'FOR THE FIRST 10 MILES',
      backgroundImage: 'linear-gradient(rgb(6 39 51 / 54%), rgb(2 27 37 / 80%)), url("/images/slider_thumbnail_1.png")'
    },
    {
      title: 'FAST RESPONSE,<br />WHEN EVERY SECOND COUNTS',
      subtitles: [
        'Emergency towing without the wait',
        'Get back on the road quickly and safely'
      ],
      price: '90$',
      priceDescription: 'WITHIN CITY LIMITS',
      backgroundImage: 'linear-gradient(rgb(6 39 51 / 54%), rgb(2 27 37 / 80%)), url("/images/photage/photage-6.jpeg")'
    },
    {
      title: 'YOUR VEHICLE\'S SAFETY,<br />IS OUR TOP PRIORITY',
      subtitles: [
        'Professional care from pickup to drop-off',
        'Trust our team to handle your car with precision'
      ],
      price: '65$',
      priceDescription: 'FLAT RATE FOR LOCAL TOWS',
      backgroundImage: 'linear-gradient(rgb(6 39 51 / 54%), rgb(2 27 37 / 80%)), url("/images/slider_thumbnail_3.png")'
    }
  ];

  constructor(private router: Router) { }

  ngOnInit(): void {
    this.initializeSlider();
  }

  ngOnDestroy(): void {
    if (this.heroSlider) {
      this.heroSlider.destroy();
    }
  }

  private initializeSlider(): void {
    // Wait for DOM to be ready
    setTimeout(() => {
      const sliderElement = document.getElementById('hero-slider');
      if (sliderElement) {
        this.heroSlider = new Splide('#hero-slider', {
          type: 'fade',
          rewind: true,
          autoplay: true,
          interval: 5000,
          speed: 1000,
          arrows: true,
          pagination: true,
          pauseOnHover: true,
          pauseOnFocus: true,
          resetProgress: false,
          height: '84vh',
          cover: true,
          lazyLoad: 'nearby'
        });
        this.heroSlider.mount();
      }
    }, 100);
  }

  onSearch(): void {
    if (this.searchTerm.trim()) {
      console.log('Searching for:', this.searchTerm);
      // Implement search functionality
      this.searchTerm = '';
    }
  }
}