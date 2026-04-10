import { Pipe, PipeTransform } from '@angular/core';
import { DatePipe } from '@angular/common';
import { TimezoneService } from '../../core/services/timezone.service';

@Pipe({
  name: 'airlineTime',
  standalone: true,
  pure: false // We need this to be impure so it re-renders immediately when the service changes
})
export class AirlineTimePipe implements PipeTransform {

  constructor(private timezoneService: TimezoneService) {}

  transform(value: any, format: string = 'medium', timezone?: string, locale?: string): any {
    if (!value) return '';
    
    // Create local DatePipe dynamically, configuring browser locale (usually en-US)
    const datePipe = new DatePipe('en-US');
    
    let currentTz = this.timezoneService.getTimezone();
    let offset = '+0000'; // Default UTC
    
    if (currentTz === 'IST') {
        offset = '+0530';
    } else if (currentTz === 'EST') {
        offset = '-0500';
    } else if (currentTz === 'UTC') {
        offset = '+0000';
    }

    // Rely on DatePipe to translate the underlying value string using the explicit offset
    return datePipe.transform(value, format, offset);
  }
}

