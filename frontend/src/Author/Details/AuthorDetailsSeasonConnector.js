/* eslint max-params: 0 */
import _ from 'lodash';
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { setAuthorDetailsId, setAuthorDetailsSort } from 'Store/Actions/authorDetailsActions';
import { setBooksTableOption, toggleBooksMonitored } from 'Store/Actions/bookActions';
import { executeCommand } from 'Store/Actions/commandActions';
import createAuthorSelector from 'Store/Selectors/createAuthorSelector';
import createClientSideCollectionSelector from 'Store/Selectors/createClientSideCollectionSelector';
import createDimensionsSelector from 'Store/Selectors/createDimensionsSelector';
import createUISettingsSelector from 'Store/Selectors/createUISettingsSelector';
import AuthorDetailsSeason from './AuthorDetailsSeason';

function createMapStateToProps() {
  return createSelector(
    createClientSideCollectionSelector('books', 'authorDetails'),
    createAuthorSelector(),
    createDimensionsSelector(),
    createUISettingsSelector(),
    (books, author, dimensions, uiSettings) => {

      const booksInGroup = books.items;

      let sortDir = 'asc';

      if (books.sortDirection === 'descending') {
        sortDir = 'desc';
      }

      const sortedBooks = _.orderBy(booksInGroup, books.sortKey, sortDir);

      return {
        items: sortedBooks,
        columns: books.columns,
        sortKey: books.sortKey,
        sortDirection: books.sortDirection,
        authorMonitored: author.monitored,
        isSmallScreen: dimensions.isSmallScreen,
        uiSettings,
        pageSize: books.pageSize,
        totalPages: books.totalPages,
        totalRecords: books.totalRecords,
        page: books.page,
        isFetching: books.isFetching
      };
    }
  );
}

const mapDispatchToProps = {
  setAuthorDetailsId,
  setAuthorDetailsSort,
  toggleBooksMonitored,
  setBooksTableOption,
  executeCommand
};

class AuthorDetailsSeasonConnector extends Component {

  //
  // Lifecycle

  componentDidMount() {
    this.props.setAuthorDetailsId({ authorId: this.props.authorId });
  }

  //
  // Listeners

  onTableOptionChange = (payload) => {
    this.props.setBooksTableOption(payload);

    if (payload.pageSize) {
      this.props.onFirstPagePress();
    }
  };

  onSortPress = (sortKey) => {
    this.props.setAuthorDetailsSort({ sortKey });
  };

  onMonitorBookPress = (bookIds, monitored) => {
    this.props.toggleBooksMonitored({
      bookIds,
      monitored
    });
  };

  onFirstPagePress = () => {
    this.props.onFirstPagePress();
  };

  onPreviousPagePress = () => {
    this.props.onPreviousPagePress();
  };

  onNextPagePress = () => {
    this.props.onNextPagePress();
  };

  onLastPagePress = () => {
    this.props.onLastPagePress();
  };

  onPageSelect = (page) => {
    this.props.onPageSelect(page);
  };

  //
  // Render

  render() {
    return (
      <AuthorDetailsSeason
        {...this.props}
        onSortPress={this.onSortPress}
        onTableOptionChange={this.onTableOptionChange}
        onMonitorBookPress={this.onMonitorBookPress}
        onFirstPagePress={this.onFirstPagePress}
        onPreviousPagePress={this.onPreviousPagePress}
        onNextPagePress={this.onNextPagePress}
        onLastPagePress={this.onLastPagePress}
        onPageSelect={this.onPageSelect}
        pageSize={this.props.pageSize}
      />
    );
  }
}

AuthorDetailsSeasonConnector.propTypes = {
  authorId: PropTypes.number.isRequired,
  toggleBooksMonitored: PropTypes.func.isRequired,
  setBooksTableOption: PropTypes.func.isRequired,
  setAuthorDetailsId: PropTypes.func.isRequired,
  setAuthorDetailsSort: PropTypes.func.isRequired,
  executeCommand: PropTypes.func.isRequired,
  onFirstPagePress: PropTypes.func.isRequired,
  onPreviousPagePress: PropTypes.func.isRequired,
  onNextPagePress: PropTypes.func.isRequired,
  onLastPagePress: PropTypes.func.isRequired,
  onPageSelect: PropTypes.func.isRequired,
  pageSize: PropTypes.number
};

export default connect(createMapStateToProps, mapDispatchToProps)(AuthorDetailsSeasonConnector);
