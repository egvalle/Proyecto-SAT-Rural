import { Component, ElementRef, ViewChild } from '@angular/core';
import { NgClass } from '@angular/common';
import { SidebarService } from '../../../../shared/services/sidebar.service';
import { RouterModule} from '@angular/router';
import { DashboardNavbarComponent } from './dashboard-navbar/dashboard-navbar.component';

@Component({
  selector: 'app-dashboard',
  imports: [
    RouterModule,
    DashboardNavbarComponent,
    // NgClass,
    // AppSidebarComponent,
    // BackdropComponent,
    // AppHeaderComponent,
],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss',
})
export class Dashboard {
  isApplicationMenuOpen = false;
  readonly isMobileOpen$;
  readonly isHovered$;
  readonly isExpanded$;
  
  @ViewChild('searchInput') searchInput!: ElementRef<HTMLInputElement>;

  constructor(public sidebarService: SidebarService) {
    this.isMobileOpen$ = this.sidebarService.isMobileOpen$;
    this.isHovered$ = this.sidebarService.isHovered$;
    this.isExpanded$ = this.sidebarService.isExpanded$;
  }

   handleToggle() {
    if (window.innerWidth >= 1280) {
      this.sidebarService.toggleExpanded();
    } else {
      this.sidebarService.toggleMobileOpen();
    }
  }

  toggleApplicationMenu() {
    this.isApplicationMenuOpen = !this.isApplicationMenuOpen;
  }

  ngAfterViewInit() {
    document.addEventListener('keydown', this.handleKeyDown);
  }

  ngOnDestroy() {
    document.removeEventListener('keydown', this.handleKeyDown);
  }

  handleKeyDown = (event: KeyboardEvent) => {
    if ((event.metaKey || event.ctrlKey) && event.key === 'k') {
      event.preventDefault();
      this.searchInput?.nativeElement.focus();
    }
  };
  // get containerClasses() {
  //   return [
  //     'flex-1',
  //     'transition-all',
  //     'duration-300',
  //     'ease-in-out',
  //     (this.isExpanded$ || this.isHovered$) ? 'xl:ml-[290px]' : 'xl:ml-[90px]',
  //     this.isMobileOpen$ ? 'ml-0' : ''
  //   ];
  // }

}
